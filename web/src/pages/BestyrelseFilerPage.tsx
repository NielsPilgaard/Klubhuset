import { useQuery } from '@tanstack/react-query'
import { usePageTitle } from '../hooks/usePageTitle'
import keycloak from '../auth/keycloak'

interface BoardFile {
  id: string
  fileName: string
  sizeBytes: number
  uploadedAt: string
}

interface BoardFolder {
  id: string
  name: string
}

interface BoardFilesResponse {
  files: BoardFile[]
  folders: BoardFolder[]
}

export default function BestyrelseFilerPage() {
  usePageTitle('Bestyrelsesdokumenter')

  const { data, isLoading } = useQuery({
    queryKey: ['board-files'],
    queryFn: async () => {
      await keycloak.updateToken(30).catch(() => keycloak.login())
      const res = await fetch('/api/v1/board-files', {
        headers: { Authorization: `Bearer ${keycloak.token}` },
      })
      if (!res.ok) throw new Error('Kunne ikke hente filer')
      return res.json() as Promise<BoardFilesResponse>
    },
  })

  return (
    <div className="p-6 lg:p-8 max-w-4xl mx-auto space-y-8">
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">Bestyrelsesdokumenter</h1>
        <p className="mt-1 text-sm text-gray-500">Dokumenter og filer til bestyrelsen</p>
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-200 p-4 animate-pulse">
              <div className="h-4 w-48 bg-gray-200 rounded" />
            </div>
          ))}
        </div>
      ) : data?.files.length === 0 && data?.folders.length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          <p>Ingen bestyrelsesdokumenter endnu</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
          {data?.folders.map((folder) => (
            <div key={folder.id} className="px-5 py-3 flex items-center gap-3">
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                className="text-gray-400 shrink-0"
              >
                <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z" />
              </svg>
              <span className="text-sm text-gray-700">{folder.name}</span>
            </div>
          ))}
          {data?.files.map((file) => (
            <div key={file.id} className="px-5 py-3 flex items-center gap-3">
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                className="text-gray-400 shrink-0"
              >
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
              <span className="text-sm text-gray-700 flex-1 truncate">{file.fileName}</span>
              <span className="text-xs text-gray-400 shrink-0">
                {new Date(file.uploadedAt).toLocaleDateString('da-DK')}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
