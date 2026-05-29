import keycloak from '../auth/keycloak'
import { API_BASE, ApiError } from './client'

export interface UploadOptions {
  file: File
  courseId?: string
  folderId?: string
  onProgress?: (pct: number) => void
}

export interface UploadedFile {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  url: string
  folderId?: string | null
}

/**
 * Uploads a file using the presigned URL flow:
 * 1. POST /files/presign  → get S3 upload URL + confirm token
 * 2. PUT directly to S3   → progress events fire here
 * 3. POST /files/confirm  → register the file in the DB
 */
export async function uploadFile({
  file,
  courseId,
  folderId,
  onProgress,
}: UploadOptions): Promise<UploadedFile> {
  await keycloak.updateToken(30).catch(() => keycloak.login())

  // Step 1: get presigned URL
  const presignRes = await fetch(`${API_BASE}/files/presign`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(keycloak.token ? { Authorization: `Bearer ${keycloak.token}` } : {}),
    },
    body: JSON.stringify({
      fileName: file.name,
      fileSizeBytes: file.size,
      courseId: courseId || null,
      folderId: folderId || null,
    }),
  })

  if (!presignRes.ok) {
    const text = await presignRes.text().catch(() => presignRes.statusText)
    throw new ApiError(presignRes.status, text)
  }

  const { uploadUrl, confirmToken } = (await presignRes.json()) as {
    fileId: string
    uploadUrl: string
    confirmToken: string
  }

  // Step 2: upload directly to S3 via XHR so we get progress events
  await new Promise<void>((resolve, reject) => {
    const xhr = new XMLHttpRequest()
    xhr.open('PUT', uploadUrl)
    xhr.setRequestHeader('Content-Type', file.type || 'application/octet-stream')

    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable && onProgress) {
        onProgress(Math.round((e.loaded / e.total) * 90)) // reserve 10% for confirm step
      }
    }

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve()
      } else {
        reject(new Error(`S3 upload fejlede: ${xhr.status} ${xhr.statusText}`))
      }
    }

    xhr.onerror = () => reject(new Error('Netværksfejl under upload til S3.'))
    xhr.send(file)
  })

  onProgress?.(95)

  // Step 3: confirm with the API
  await keycloak.updateToken(30).catch(() => keycloak.login())
  const confirmRes = await fetch(`${API_BASE}/files/confirm`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(keycloak.token ? { Authorization: `Bearer ${keycloak.token}` } : {}),
    },
    body: JSON.stringify({ confirmToken }),
  })

  if (!confirmRes.ok) {
    const text = await confirmRes.text().catch(() => confirmRes.statusText)
    throw new ApiError(confirmRes.status, text)
  }

  onProgress?.(100)
  return confirmRes.json() as Promise<UploadedFile>
}
