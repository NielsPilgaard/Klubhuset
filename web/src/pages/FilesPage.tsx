import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, CourseDto } from '../api/client'
import { uploadFile } from '../api/upload'
import { usePageTitle } from '../hooks/usePageTitle'
import keycloak from '../auth/keycloak'

// ─── Types ────────────────────────────────────────────────────────────────────

interface FileDto {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  url: string
  courseId?: string | null
  courseName?: string | null
  folderId?: string | null
  uploadedBy: string
  uploadedAt: string
}

interface FolderDto {
  id: string
  name: string
  parentId?: string | null
  createdAt: string
}

interface FilesResponse {
  files: FileDto[]
  folders: FolderDto[]
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('da-DK', { day: '2-digit', month: 'short', year: 'numeric' })
}

function fileIcon(contentType: string) {
  if (contentType.startsWith('image/')) {
    return (
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="18" height="18" rx="2" />
        <circle cx="8.5" cy="8.5" r="1.5" />
        <polyline points="21 15 16 10 5 21" />
      </svg>
    )
  }
  if (contentType.startsWith('video/')) {
    return (
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <polygon points="23 7 16 12 23 17 23 7" />
        <rect x="1" y="5" width="15" height="14" rx="2" ry="2" />
      </svg>
    )
  }
  if (contentType.startsWith('audio/')) {
    return (
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M9 18V5l12-2v13" />
        <circle cx="6" cy="18" r="3" />
        <circle cx="18" cy="16" r="3" />
      </svg>
    )
  }
  if (contentType.includes('pdf')) {
    return (
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <polyline points="14 2 14 8 20 8" />
        <line x1="16" y1="13" x2="8" y2="13" />
        <line x1="16" y1="17" x2="8" y2="17" />
      </svg>
    )
  }
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
    </svg>
  )
}

function folderIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-yellow-500">
      <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z" />
    </svg>
  )
}

// ─── Upload modal ─────────────────────────────────────────────────────────────

interface UploadModalProps {
  courses: CourseDto[]
  currentFolderId: string | null
  onClose: () => void
  onUploaded: () => void
}

function UploadModal({ courses, currentFolderId, onClose, onUploaded }: UploadModalProps) {
  const qc = useQueryClient()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [courseId, setCourseId] = useState<string>('')
  const [progress, setProgress] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const dragRef = useRef(false)
  const [isDragging, setIsDragging] = useState(false)

  const isPending = progress !== null && progress < 100

  async function handleUpload() {
    if (!selectedFile || isPending) return
    setError(null)
    setProgress(0)

    try {
      await uploadFile({
        file: selectedFile,
        courseId: courseId || undefined,
        folderId: currentFolderId || undefined,
        onProgress: setProgress,
      })
      qc.invalidateQueries({ queryKey: ['files'] })
      onUploaded()
    } catch (err: unknown) {
      setProgress(null)
      try {
        const body = JSON.parse((err as Error).message)
        const msgs = Object.values(body.errors ?? {}).flat() as string[]
        setError(msgs[0] ?? 'Der opstod en fejl.')
      } catch {
        setError('Der opstod en fejl under upload. Prøv igen.')
      }
    }
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    dragRef.current = false
    const f = e.dataTransfer.files[0]
    if (f) { setError(null); setSelectedFile(f) }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Upload fil</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          {/* Drop zone */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Fil *</label>
            <div
              onClick={() => !isPending && inputRef.current?.click()}
              onDragEnter={(e) => { e.preventDefault(); if (!dragRef.current) { dragRef.current = true; setIsDragging(true) } }}
              onDragOver={(e) => e.preventDefault()}
              onDragLeave={(e) => { if (!e.currentTarget.contains(e.relatedTarget as Node)) { dragRef.current = false; setIsDragging(false) } }}
              onDrop={handleDrop}
              className={`flex flex-col items-center justify-center border-2 border-dashed rounded-lg p-6 transition-colors ${
                isPending ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'
              } ${isDragging ? 'border-brand-400 bg-brand-50' : 'border-gray-300 hover:border-brand-400'}`}
            >
              {selectedFile ? (
                <div className="text-center w-full">
                  <p className="text-sm font-medium text-gray-900 truncate">{selectedFile.name}</p>
                  <p className="text-xs text-gray-500 mt-1">{formatBytes(selectedFile.size)}</p>
                  {progress !== null && (
                    <div className="mt-3">
                      <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
                        <div
                          className="h-full bg-brand-500 rounded-full transition-all duration-300"
                          style={{ width: `${progress}%` }}
                        />
                      </div>
                      <p className="text-xs text-gray-500 mt-1">
                        {progress < 100 ? `${progress}% uploadet…` : 'Fuldfører…'}
                      </p>
                    </div>
                  )}
                </div>
              ) : (
                <div className="text-center">
                  <svg className="mx-auto mb-2 text-gray-400" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="17 8 12 3 7 8" />
                    <line x1="12" y1="3" x2="12" y2="15" />
                  </svg>
                  <p className="text-sm text-gray-500">Klik eller træk fil hertil</p>
                  <p className="text-xs text-gray-400 mt-1">PDF, Word, Excel, video, lyd, billeder m.m. · Maks. 500 MB</p>
                </div>
              )}
            </div>
            <input
              ref={inputRef}
              type="file"
              className="hidden"
              onChange={(e) => { setError(null); setProgress(null); setSelectedFile(e.target.files?.[0] ?? null) }}
            />
          </div>

          {/* Course */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Tilknyt fag (valgfrit)</label>
            <select
              value={courseId}
              onChange={(e) => setCourseId(e.target.value)}
              disabled={isPending}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
            >
              <option value="">Intet fag</option>
              {courses.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} disabled={isPending} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 disabled:opacity-50">
            Annuller
          </button>
          <button
            onClick={handleUpload}
            disabled={!selectedFile || isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isPending ? `${progress}%…` : 'Upload'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── New folder modal ─────────────────────────────────────────────────────────

interface NewFolderModalProps {
  parentId: string | null
  onClose: () => void
  onCreated: () => void
}

function NewFolderModal({ parentId, onClose, onCreated }: NewFolderModalProps) {
  const qc = useQueryClient()
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => api.post('/files/folders', { name: name.trim(), parentId: parentId || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['files'] })
      onCreated()
    },
    onError: () => setError('Kunne ikke oprette mappen. Prøv igen.'),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Ny mappe</h2>
        </div>
        <div className="px-6 py-5">
          <label className="block text-sm font-medium text-gray-700 mb-1">Mappenavn *</label>
          <input
            autoFocus
            type="text"
            value={name}
            onChange={(e) => { setName(e.target.value); setError(null) }}
            onKeyDown={(e) => { if (e.key === 'Enter' && name.trim()) mutation.mutate() }}
            placeholder="F.eks. Matematik"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
          />
          {error && <p className="text-sm text-red-600 mt-2">{error}</p>}
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900">Annuller</button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!name.trim() || mutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Opretter…' : 'Opret'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function FilesPage() {
  usePageTitle('Filer')
  const qc = useQueryClient()
  const isAdmin = keycloak.hasRealmRole('admin')

  const [currentFolderId, setCurrentFolderId] = useState<string | null>(null)
  const [breadcrumb, setBreadcrumb] = useState<FolderDto[]>([])
  const [filterCourseId, setFilterCourseId] = useState<string>('')
  const [showUpload, setShowUpload] = useState(false)
  const [showNewFolder, setShowNewFolder] = useState(false)

  const { data: courses } = useQuery<CourseDto[]>({
    queryKey: ['courses'],
    queryFn: () => api.get('/courses'),
  })

  const { data, isLoading, isError, refetch } = useQuery<FilesResponse>({
    queryKey: ['files', currentFolderId, filterCourseId],
    queryFn: () => {
      const params = new URLSearchParams()
      if (currentFolderId) params.set('folderId', currentFolderId)
      if (filterCourseId) params.set('courseId', filterCourseId)
      const qs = params.toString()
      return api.get(`/files${qs ? `?${qs}` : ''}`)
    },
  })

  const deleteFileMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/files/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['files'] }),
  })

  const deleteFolderMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/files/folders/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['files'] }),
  })

  function openFolder(folder: FolderDto) {
    setCurrentFolderId(folder.id)
    setBreadcrumb((prev) => [...prev, folder])
    setFilterCourseId('')
  }

  function navigateBreadcrumb(index: number) {
    if (index === -1) {
      setCurrentFolderId(null)
      setBreadcrumb([])
    } else {
      const target = breadcrumb[index]
      setCurrentFolderId(target.id)
      setBreadcrumb((prev) => prev.slice(0, index + 1))
    }
  }

  const folders = data?.folders ?? []
  const files = data?.files ?? []
  const isEmpty = !isLoading && folders.length === 0 && files.length === 0

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Filer</h1>
          <p className="mt-1 text-sm text-gray-500">Filer og mapper tilknyttet skolen og dens fag</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowNewFolder(true)}
            className="flex items-center gap-2 px-3 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z" />
              <line x1="12" y1="11" x2="12" y2="17" />
              <line x1="9" y1="14" x2="15" y2="14" />
            </svg>
            Ny mappe
          </button>
          <button
            onClick={() => setShowUpload(true)}
            className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="17 8 12 3 7 8" />
              <line x1="12" y1="3" x2="12" y2="15" />
            </svg>
            Upload fil
          </button>
        </div>
      </div>

      {/* Breadcrumb + course filter row */}
      <div className="flex items-center justify-between gap-4 flex-wrap">
        {/* Breadcrumb */}
        <nav className="flex items-center gap-1 text-sm min-w-0">
          <button
            onClick={() => navigateBreadcrumb(-1)}
            className={`flex items-center gap-1 hover:text-brand-600 transition-colors ${breadcrumb.length === 0 ? 'text-gray-900 font-medium' : 'text-gray-500'}`}
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
              <polyline points="9 22 9 12 15 12 15 22" />
            </svg>
            Alle filer
          </button>
          {breadcrumb.map((folder, i) => (
            <span key={folder.id} className="flex items-center gap-1 min-w-0">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-gray-300 shrink-0">
                <polyline points="9 18 15 12 9 6" />
              </svg>
              <button
                onClick={() => navigateBreadcrumb(i)}
                className={`truncate hover:text-brand-600 transition-colors ${i === breadcrumb.length - 1 ? 'text-gray-900 font-medium' : 'text-gray-500'}`}
              >
                {folder.name}
              </button>
            </span>
          ))}
        </nav>

        {/* Course filter (only at root) */}
        {!currentFolderId && (
          <div className="flex items-center gap-2 shrink-0">
            <label className="text-sm text-gray-600">Filtrer efter fag:</label>
            <select
              value={filterCourseId}
              onChange={(e) => setFilterCourseId(e.target.value)}
              className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            >
              <option value="">Alle fag</option>
              {courses?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            {filterCourseId && (
              <button onClick={() => setFilterCourseId('')} className="text-xs text-brand-600 hover:text-brand-800">
                Ryd filter
              </button>
            )}
          </div>
        )}
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 flex items-center justify-between">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente filer</p>
          <button onClick={() => refetch()} className="text-sm px-3 py-1.5 bg-red-100 text-red-700 rounded-lg">Prøv igen</button>
        </div>
      )}

      {/* File / folder listing */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Navn</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden md:table-cell">Fag</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden sm:table-cell">Dato</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden lg:table-cell">Af</th>
              <th className="px-5 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Handlinger</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  <td className="px-5 py-3"><div className="h-4 w-40 bg-gray-200 rounded" /></td>
                  <td className="px-5 py-3 hidden md:table-cell"><div className="h-4 w-24 bg-gray-100 rounded" /></td>
                  <td className="px-5 py-3 hidden sm:table-cell"><div className="h-4 w-20 bg-gray-100 rounded" /></td>
                  <td className="px-5 py-3 hidden lg:table-cell"><div className="h-4 w-24 bg-gray-100 rounded" /></td>
                  <td className="px-5 py-3" />
                </tr>
              ))}

            {/* Folders */}
            {folders.map((folder) => (
              <tr
                key={`folder-${folder.id}`}
                className="hover:bg-gray-50 transition-colors cursor-pointer"
                data-testid={`folder-row-${folder.id}`}
                onClick={() => openFolder(folder)}
              >
                <td className="px-5 py-3">
                  <div className="flex items-center gap-2">
                    <span className="shrink-0">{folderIcon()}</span>
                    <span className="font-medium text-gray-900 truncate">{folder.name}</span>
                  </div>
                </td>
                <td className="px-5 py-3 hidden md:table-cell"><span className="text-gray-300">—</span></td>
                <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">{formatDate(folder.createdAt)}</td>
                <td className="px-5 py-3 hidden lg:table-cell"><span className="text-gray-300">—</span></td>
                <td className="px-5 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                  {isAdmin && (
                    <button
                      data-testid={`delete-folder-${folder.id}`}
                      onClick={() => {
                        if (confirm(`Slet mappen "${folder.name}" og alle dens indhold?`)) {
                          deleteFolderMutation.mutate(folder.id)
                        }
                      }}
                      className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                      title="Slet mappe"
                    >
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6M14 11v6" />
                        <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                      </svg>
                    </button>
                  )}
                </td>
              </tr>
            ))}

            {/* Files */}
            {files.map((f) => (
              <tr key={f.id} className="hover:bg-gray-50 transition-colors" data-testid={`file-row-${f.id}`}>
                <td className="px-5 py-3">
                  <div className="flex items-center gap-2">
                    <span className="text-gray-400 shrink-0">{fileIcon(f.contentType ?? '')}</span>
                    <div className="min-w-0">
                      <span className="font-medium text-gray-900 truncate block">{f.fileName}</span>
                      <span className="text-xs text-gray-400">{formatBytes(f.sizeBytes ?? 0)}</span>
                    </div>
                  </div>
                </td>
                <td className="px-5 py-3 text-gray-500 hidden md:table-cell">
                  {f.courseName ?? <span className="text-gray-300">—</span>}
                </td>
                <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">
                  {formatDate(f.uploadedAt ?? new Date().toISOString())}
                </td>
                <td className="px-5 py-3 text-gray-500 hidden lg:table-cell">
                  {f.uploadedBy ?? '—'}
                </td>
                <td className="px-5 py-3 text-right">
                  <div className="flex items-center justify-end gap-1">
                    <a
                      href={f.url || undefined}
                      target="_blank"
                      rel="noopener noreferrer"
                      download={f.fileName}
                      data-testid={`download-${f.id}`}
                      className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                      title="Hent fil"
                    >
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                        <polyline points="7 10 12 15 17 10" />
                        <line x1="12" y1="15" x2="12" y2="3" />
                      </svg>
                    </a>
                    {isAdmin && (
                      <button
                        data-testid={`delete-${f.id}`}
                        onClick={() => {
                          if (confirm(`Slet filen "${f.fileName ?? 'fil'}"?`)) deleteFileMutation.mutate(f.id)
                        }}
                        className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                        title="Slet fil"
                      >
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <polyline points="3 6 5 6 21 6" />
                          <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                          <path d="M10 11v6M14 11v6" />
                          <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                        </svg>
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}

            {isEmpty && (
              <tr>
                <td colSpan={5} className="px-5 py-12 text-center">
                  <p className="text-gray-400 text-sm">Ingen filer eller mapper her endnu</p>
                  <p className="text-gray-300 text-xs mt-1">Klik "Upload fil" for at komme i gang</p>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {showUpload && (
        <UploadModal
          courses={courses ?? []}
          currentFolderId={currentFolderId}
          onClose={() => setShowUpload(false)}
          onUploaded={() => setShowUpload(false)}
        />
      )}

      {showNewFolder && (
        <NewFolderModal
          parentId={currentFolderId}
          onClose={() => setShowNewFolder(false)}
          onCreated={() => setShowNewFolder(false)}
        />
      )}
    </div>
  )
}
