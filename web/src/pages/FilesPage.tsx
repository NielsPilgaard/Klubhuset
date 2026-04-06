import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, FileDto, CourseDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'
import keycloak from '../auth/keycloak'

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
  if (contentType.includes('pdf')) {
    return (
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <polyline points="14 2 14 8 20 8" />
        <line x1="16" y1="13" x2="8" y2="13" />
        <line x1="16" y1="17" x2="8" y2="17" />
        <polyline points="10 9 9 9 8 9" />
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

interface UploadModalProps {
  courses: CourseDto[]
  onClose: () => void
  onUploaded: () => void
}

function UploadModal({ courses, onClose, onUploaded }: UploadModalProps) {
  const qc = useQueryClient()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [courseId, setCourseId] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const mutation = useMutation({
    mutationFn: () => {
      if (!selectedFile) throw new Error('Ingen fil valgt')
      const form = new FormData()
      form.append('file', selectedFile)
      if (courseId) form.append('courseId', courseId)
      return api.postForm<FileDto>('/files', form)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['files'] })
      onUploaded()
    },
    onError: (err: Error) => {
      try {
        const body = JSON.parse(err.message)
        const msgs = Object.values(body.errors ?? {}).flat() as string[]
        setError(msgs[0] ?? 'Der opstod en fejl.')
      } catch {
        setError('Der opstod en fejl. Prøv igen.')
      }
    },
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Upload fil</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Fil *</label>
            <div
              onClick={() => inputRef.current?.click()}
              className="flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-lg p-6 cursor-pointer hover:border-brand-400 transition-colors"
            >
              {selectedFile ? (
                <div className="text-center">
                  <p className="text-sm font-medium text-gray-900">{selectedFile.name}</p>
                  <p className="text-xs text-gray-500 mt-1">{formatBytes(selectedFile.size)}</p>
                </div>
              ) : (
                <div className="text-center">
                  <svg className="mx-auto mb-2 text-gray-400" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                    <polyline points="17 8 12 3 7 8" />
                    <line x1="12" y1="3" x2="12" y2="15" />
                  </svg>
                  <p className="text-sm text-gray-500">Klik for at vælge fil</p>
                  <p className="text-xs text-gray-400 mt-1">PDF, Word, Excel, billeder m.m. · Maks. 100 MB</p>
                </div>
              )}
            </div>
            <input
              ref={inputRef}
              type="file"
              className="hidden"
              onChange={(e) => {
                setError(null)
                setSelectedFile(e.target.files?.[0] ?? null)
              }}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Tilknyt fag (valgfrit)</label>
            <select
              value={courseId}
              onChange={(e) => setCourseId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
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
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900">
            Annuller
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={!selectedFile || mutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Uploader...' : 'Upload'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default function FilesPage() {
  usePageTitle('Filer')
  const qc = useQueryClient()
  const isAdmin = keycloak.hasRealmRole('admin')

  const [showUpload, setShowUpload] = useState(false)
  const [filterCourseId, setFilterCourseId] = useState<string>('')

  const { data: courses } = useQuery<CourseDto[]>({
    queryKey: ['courses'],
    queryFn: () => api.get('/courses'),
  })

  const { data: files, isLoading, isError, refetch } = useQuery<FileDto[]>({
    queryKey: ['files', filterCourseId],
    queryFn: () => api.get(`/files${filterCourseId ? `?courseId=${filterCourseId}` : ''}`),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/files/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['files'] }),
  })

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Filer</h1>
          <p className="mt-1 text-sm text-gray-500">Filer tilknyttet skolen og dens fag</p>
        </div>
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

      {/* Filter by course */}
      <div className="flex items-center gap-3">
        <label className="text-sm text-gray-600 shrink-0">Filtrer efter fag:</label>
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
          <button
            onClick={() => setFilterCourseId('')}
            className="text-xs text-brand-600 hover:text-brand-800"
          >
            Ryd filter
          </button>
        )}
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 flex items-center justify-between">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente filer</p>
          <button onClick={() => refetch()} className="text-sm px-3 py-1.5 bg-red-100 text-red-700 rounded-lg">
            Prøv igen
          </button>
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Filnavn</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden md:table-cell">Fag</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden sm:table-cell">Uploadet</th>
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
            {!isLoading && files?.length === 0 && (
              <tr>
                <td colSpan={5} className="px-5 py-10 text-center text-gray-400">
                  Ingen filer uploadet endnu
                </td>
              </tr>
            )}
            {files?.map((f) => (
              <tr key={f.id} className="hover:bg-gray-50 transition-colors" data-testid={`file-row-${f.id}`}>
                <td className="px-5 py-3">
                  <div className="flex items-center gap-2">
                    <span className="text-gray-400 shrink-0">{fileIcon(f.contentType)}</span>
                    <div className="min-w-0">
                      <span className="font-medium text-gray-900 truncate block">{f.fileName}</span>
                      <span className="text-xs text-gray-400">{formatBytes(f.sizeBytes)}</span>
                    </div>
                  </div>
                </td>
                <td className="px-5 py-3 text-gray-500 hidden md:table-cell">
                  {f.courseName ?? <span className="text-gray-300">—</span>}
                </td>
                <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">
                  {formatDate(f.uploadedAt)}
                </td>
                <td className="px-5 py-3 text-gray-500 hidden lg:table-cell">
                  {f.uploadedBy}
                </td>
                <td className="px-5 py-3 text-right">
                  <div className="flex items-center justify-end gap-1">
                    <a
                      href={f.url}
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
                          if (confirm(`Slet filen "${f.fileName}"?`)) deleteMutation.mutate(f.id)
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
          </tbody>
        </table>
      </div>

      {showUpload && (
        <UploadModal
          courses={courses ?? []}
          onClose={() => setShowUpload(false)}
          onUploaded={() => setShowUpload(false)}
        />
      )}
    </div>
  )
}
