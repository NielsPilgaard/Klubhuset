import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getApiV1CoursesOptions,
  getApiV1FilesOptions,
  getApiV1FilesQueryKey,
  deleteApiV1FilesByIdMutation,
  postApiV1FilesFoldersMutation,
  deleteApiV1FilesFoldersByIdMutation,
  patchApiV1FilesFoldersByIdMutation,
} from '../../api/generated/@tanstack/react-query.gen'
import { postApiV1FilesPresign, postApiV1FilesConfirm } from '../../api/generated/sdk.gen'
import type { CourseDto, FolderDto } from '../../api/client'
import keycloak from '../../auth/keycloak'
import {
  FileIcon,
  FolderIcon,
  ChevronIcon,
  PencilIcon,
  TrashIcon,
  UploadIcon,
  DownloadIcon,
  EyeIcon,
  CloseIcon,
} from './fileIcons'
import { formatBytes, formatDate } from './fileHelpers'

// ─── Upload modal ─────────────────────────────────────────────────────────────

interface UploadModalProps {
  courses: CourseDto[] | undefined
  currentFolderId: string | null
  defaultCourseId?: string
  onClose: () => void
  onUploaded: (courseId: string) => void
}

function UploadModal({ courses, currentFolderId, defaultCourseId, onClose, onUploaded }: UploadModalProps) {
  const qc = useQueryClient()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [editedName, setEditedName] = useState<string>('')
  const [courseId, setCourseId] = useState<string>(defaultCourseId ?? '')
  const [progress, setProgress] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const dragRef = useRef(false)
  const [isDragging, setIsDragging] = useState(false)

  const mutation = useMutation({
    mutationFn: async ({ file }: { file: File }) => {
      const ext = file.name.includes('.') ? '.' + file.name.split('.').pop()! : ''
      const { data: presign } = await postApiV1FilesPresign({
        body: {
          fileName: editedName + ext,
          fileSizeBytes: file.size,
          courseId: courseId || undefined,
          folderId: currentFolderId || undefined,
        },
        throwOnError: true,
      })
      await fetch(presign!.uploadUrl!, {
        method: 'PUT',
        body: file,
        headers: { 'Content-Type': presign!.contentType! },
      })
      setProgress(100)
      const { data: confirmed } = await postApiV1FilesConfirm({
        body: { confirmToken: presign!.confirmToken },
        throwOnError: true,
      })
      return confirmed
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: getApiV1FilesQueryKey() })
      onUploaded(courseId)
    },
    onError: () => {
      setProgress(null)
      setError('Der opstod en fejl under upload. Prøv igen.')
    },
  })

  const isPending = mutation.isPending

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    dragRef.current = false
    const f = e.dataTransfer.files[0]
    if (f) { setError(null); setSelectedFile(f); setEditedName(f.name.replace(/\.[^.]+$/, '')) }
  }

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
                        <div className="h-full bg-brand-500 rounded-full transition-all duration-300" style={{ width: `${progress}%` }} />
                      </div>
                      <p className="text-xs text-gray-500 mt-1">{progress < 100 ? `${progress}% uploadet…` : 'Fuldfører…'}</p>
                    </div>
                  )}
                </div>
              ) : (
                <div className="text-center">
                  <UploadIcon size={24} />
                  <p className="text-sm text-gray-500 mt-2">Klik eller træk fil hertil</p>
                  <p className="text-xs text-gray-400 mt-1">PDF, Word, Excel, video, lyd, billeder m.m. · Maks. 500 MB</p>
                </div>
              )}
            </div>
            <input
              ref={inputRef}
              type="file"
              accept="*/*"
              className="hidden"
              onChange={(e) => {
                const f = e.target.files?.[0] ?? null
                setError(null); setProgress(null); setSelectedFile(f)
                if (f) setEditedName(f.name.replace(/\.[^.]+$/, ''))
              }}
            />
          </div>

          {selectedFile && (
            <div>
              <label htmlFor="upload-filename" className="block text-sm font-medium text-gray-700 mb-1">Filnavn</label>
              <input
                id="upload-filename"
                type="text"
                value={editedName}
                onChange={(e) => setEditedName(e.target.value)}
                disabled={isPending}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Tilknyt fag (valgfrit)</label>
            <select
              value={courseId}
              onChange={(e) => setCourseId(e.target.value)}
              disabled={isPending}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
            >
              <option value="">Intet fag</option>
              {courses?.map((c) => (
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
            onClick={() => { if (selectedFile) mutation.mutate({ file: selectedFile }) }}
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

// ─── File preview modal ───────────────────────────────────────────────────────

interface FilePreviewModalProps {
  fileName: string
  contentType: string
  url: string
  onClose: () => void
}

function FilePreviewModal({ fileName, contentType, url, onClose }: FilePreviewModalProps) {
  const isImage = contentType.startsWith('image/')
  const isPdf = contentType.includes('pdf')

  if (isPdf) {
    window.open(url, '_blank', 'noopener,noreferrer')
    onClose()
    return null
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-[95vw] h-[92vh] flex flex-col" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between gap-4 shrink-0">
          <span className="font-medium text-gray-900 truncate">{fileName}</span>
          <button
            onClick={onClose}
            className="shrink-0 p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
            title="Luk"
          >
            <CloseIcon />
          </button>
        </div>
        <div className="flex-1 flex flex-col min-h-0 overflow-hidden">
          {isImage && (
            <div className="flex-1 overflow-auto flex items-center justify-center p-4 min-h-0">
              <img src={url} alt={fileName} className="max-h-full max-w-full object-contain mx-auto" />
            </div>
          )}
          {!isImage && (
            <div className="flex-1 flex flex-col items-center justify-center gap-3">
              <p className="text-gray-700 font-medium">{fileName}</p>
              <p className="text-sm text-gray-400">{contentType || 'Ukendt filtype'}</p>
              <a
                href={url}
                download={fileName}
                className="inline-flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
              >
                Hent fil
              </a>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Create folder modal ──────────────────────────────────────────────────────

interface CreateFolderModalProps {
  parentId: string | null
  courses: CourseDto[] | undefined
  defaultCourseId?: string
  onClose: () => void
  onCreated: (folder: FolderDto) => void
}

function CreateFolderModal({ parentId, courses, defaultCourseId, onClose, onCreated }: CreateFolderModalProps) {
  const [name, setName] = useState('')
  const [courseId, setCourseId] = useState<string>(defaultCourseId ?? '')
  const [error, setError] = useState<string | null>(null)

  const { mutationFn } = postApiV1FilesFoldersMutation()
  const mutation = useMutation({
    mutationFn,
    onSuccess: (data) => { if (data) onCreated(data) },
    onError: () => setError('Kunne ikke oprette mappen. Prøv igen.'),
  })

  function submit() {
    const trimmed = name.trim()
    if (!trimmed) { setError('Mappenavn må ikke være tomt.'); return }
    setError(null)
    mutation.mutate({ body: { name: trimmed, parentId: parentId || undefined, courseId: courseId || undefined } })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Opret mappe</h2>
        </div>
        <div className="px-6 py-5">
          <label className="block text-sm font-medium text-gray-700 mb-1">Mappenavn *</label>
          <input
            autoFocus
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') submit() }}
            placeholder="F.eks. Matematik, Uge 10…"
            disabled={mutation.isPending}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
          />
          {error && <p className="text-sm text-red-600 mt-2">{error}</p>}
          <div className="mt-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Tilknyt fag (valgfrit)</label>
            <select
              value={courseId}
              onChange={(e) => setCourseId(e.target.value)}
              disabled={mutation.isPending}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:opacity-50"
            >
              <option value="">Intet fag</option>
              {courses?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} disabled={mutation.isPending} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 disabled:opacity-50">
            Annuller
          </button>
          <button
            onClick={submit}
            disabled={mutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Opretter…' : 'Opret'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Inline folder rename ─────────────────────────────────────────────────────

interface InlineRenameProps {
  folder: FolderDto
  onDone: () => void
}

function InlineRename({ folder, onDone }: InlineRenameProps) {
  const qc = useQueryClient()
  const [value, setValue] = useState(folder.name ?? '')
  const { mutationFn } = patchApiV1FilesFoldersByIdMutation()
  const mutation = useMutation({
    mutationFn,
    onSuccess: () => { qc.invalidateQueries({ queryKey: getApiV1FilesQueryKey() }); onDone() },
  })

  function submit() {
    const trimmed = value.trim()
    if (!trimmed || trimmed === folder.name) { onDone(); return }
    mutation.mutate({ path: { id: folder.id! }, body: { name: trimmed } })
  }

  return (
    <input
      autoFocus
      type="text"
      value={value}
      onChange={(e) => setValue(e.target.value)}
      onBlur={submit}
      onKeyDown={(e) => { if (e.key === 'Enter') submit(); if (e.key === 'Escape') onDone() }}
      disabled={mutation.isPending}
      className="px-2 py-0.5 border border-brand-400 rounded text-sm font-medium text-gray-900 focus:outline-none focus:ring-2 focus:ring-brand-500 w-40 disabled:opacity-50"
      onClick={(e) => e.stopPropagation()}
    />
  )
}

// ─── Breadcrumb ───────────────────────────────────────────────────────────────

interface BreadcrumbProps {
  trail: { id: string; name: string }[]
  onNavigate: (folderId: string | null) => void
}

function Breadcrumb({ trail, onNavigate }: BreadcrumbProps) {
  return (
    <nav className="flex items-center gap-1 text-sm flex-wrap">
      <button
        onClick={() => onNavigate(null)}
        className="text-brand-600 hover:text-brand-800 font-medium transition-colors"
        data-testid="breadcrumb-root"
      >
        Filer
      </button>
      {trail.map((crumb) => (
        <span key={crumb.id} className="flex items-center gap-1">
          <span className="text-gray-400"><ChevronIcon /></span>
          <button
            onClick={() => onNavigate(crumb.id)}
            className="text-brand-600 hover:text-brand-800 font-medium transition-colors"
            data-testid={`breadcrumb-${crumb.id}`}
          >
            {crumb.name}
          </button>
        </span>
      ))}
    </nav>
  )
}

// ─── FileSystemBrowser ────────────────────────────────────────────────────────

export interface FileSystemBrowserProps {
  /** Show full page header with title, upload button, and folder controls. Default: true */
  showHeader?: boolean
}

export function FileSystemBrowser({ showHeader = true }: FileSystemBrowserProps) {
  const qc = useQueryClient()
  const isAdmin = keycloak.hasRealmRole('admin')

  const [filterCourseId, setFilterCourseId] = useState<string>('')
  const [showUpload, setShowUpload] = useState(false)
  const [showCreateFolder, setShowCreateFolder] = useState(false)
  const [previewFile, setPreviewFile] = useState<{ fileName: string; contentType: string; url: string } | null>(null)
  const [renamingFolderId, setRenamingFolderId] = useState<string | null>(null)
  const [folderTrail, setFolderTrail] = useState<{ id: string; name: string }[]>([])

  const currentFolderId = folderTrail.length > 0 ? folderTrail[folderTrail.length - 1].id : null

  const { data: courses } = useQuery(getApiV1CoursesOptions())

  const { data, isLoading, isError, refetch } = useQuery(
    getApiV1FilesOptions({
      query: {
        ...(filterCourseId ? { courseId: filterCourseId } : {}),
        ...(currentFolderId ? { folderId: currentFolderId } : {}),
      },
    })
  )

  const { mutationFn: deleteFileMutationFn } = deleteApiV1FilesByIdMutation()
  const deleteMutation = useMutation({
    mutationFn: deleteFileMutationFn,
    onSuccess: () => qc.invalidateQueries({ queryKey: getApiV1FilesQueryKey() }),
  })

  const { mutationFn: deleteFolderMutationFn } = deleteApiV1FilesFoldersByIdMutation()
  const deleteFolderMutation = useMutation({
    mutationFn: deleteFolderMutationFn,
    onSuccess: () => qc.invalidateQueries({ queryKey: getApiV1FilesQueryKey() }),
  })

  const files = data?.files ?? []
  const folders = data?.folders ?? []
  const isEmpty = !isLoading && files.length === 0 && folders.length === 0

  function navigateInto(folder: FolderDto) {
    setFolderTrail((prev) => [...prev, { id: folder.id!, name: folder.name! }])
  }

  function navigateTo(folderId: string | null) {
    if (folderId === null) {
      setFolderTrail([])
    } else {
      const idx = folderTrail.findIndex((f) => f.id === folderId)
      if (idx !== -1) setFolderTrail(folderTrail.slice(0, idx + 1))
    }
  }

  function handleFolderCreated() {
    qc.invalidateQueries({ queryKey: getApiV1FilesQueryKey() })
    setShowCreateFolder(false)
  }

  function handleDeleteFolder(folder: FolderDto) {
    if (confirm(`Slet mappen "${folder.name ?? 'mappe'}"? Indholdet i mappen slettes også.`)) {
      deleteFolderMutation.mutate({ path: { id: folder.id! } })
    }
  }

  return (
    <div className="space-y-6">
      {showHeader && (
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div className="min-w-0">
            <h1 className="font-display text-2xl font-semibold text-gray-900">Filer</h1>
            <p className="mt-1 text-sm text-gray-500">Filer og mapper tilknyttet skolen og dens fag</p>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            {isAdmin && (
              <button
                onClick={() => setShowCreateFolder(true)}
                className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
                data-testid="create-folder-btn"
              >
                <FolderIcon size={16} />
                Ny mappe
              </button>
            )}
            <button
              onClick={() => setShowUpload(true)}
              className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
            >
              <UploadIcon size={16} />
              Upload fil
            </button>
          </div>
        </div>
      )}

      {folderTrail.length > 0 && (
        <Breadcrumb trail={folderTrail} onNavigate={navigateTo} />
      )}

      <div className="flex items-center gap-2">
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

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 flex items-center justify-between">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente filer</p>
          <button onClick={() => refetch()} className="text-sm px-3 py-1.5 bg-red-100 text-red-700 rounded-lg">Prøv igen</button>
        </div>
      )}

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
            {isLoading && Array.from({ length: 4 }).map((_, i) => (
              <tr key={i} className="animate-pulse">
                <td className="px-5 py-3"><div className="h-4 w-40 bg-gray-200 rounded" /></td>
                <td className="px-5 py-3 hidden md:table-cell"><div className="h-4 w-24 bg-gray-100 rounded" /></td>
                <td className="px-5 py-3 hidden sm:table-cell"><div className="h-4 w-20 bg-gray-100 rounded" /></td>
                <td className="px-5 py-3 hidden lg:table-cell"><div className="h-4 w-24 bg-gray-100 rounded" /></td>
                <td className="px-5 py-3" />
              </tr>
            ))}

            {folders.map((folder) => (
              <tr
                key={`folder-${folder.id}`}
                className="hover:bg-amber-50 transition-colors cursor-pointer"
                data-testid={`folder-row-${folder.id}`}
                onClick={() => navigateInto(folder)}
              >
                <td className="px-5 py-3">
                  <div className="flex items-center gap-2">
                    <span className="text-amber-400 shrink-0"><FolderIcon size={16} /></span>
                    <div className="min-w-0 flex items-center gap-2">
                      {renamingFolderId === folder.id ? (
                        <InlineRename folder={folder} onDone={() => setRenamingFolderId(null)} />
                      ) : (
                        <span className="font-medium text-gray-900 truncate">{folder.name}</span>
                      )}
                    </div>
                  </div>
                </td>
                <td className="px-5 py-3 text-gray-500 hidden md:table-cell">
                  {folder.courseName ?? <span className="text-gray-300">—</span>}
                </td>
                <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">
                  {folder.createdAt ? formatDate(folder.createdAt) : '—'}
                </td>
                <td className="px-5 py-3 hidden lg:table-cell" />
                <td className="px-5 py-3 text-right">
                  {isAdmin && (
                    <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
                      <button
                        onClick={() => setRenamingFolderId(folder.id!)}
                        className="p-1.5 text-gray-400 hover:text-brand-600 rounded-md hover:bg-brand-50 transition-colors"
                        title="Omdøb mappe"
                        data-testid={`rename-folder-${folder.id}`}
                      >
                        <PencilIcon />
                      </button>
                      <button
                        onClick={() => handleDeleteFolder(folder)}
                        className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                        title="Slet mappe"
                        data-testid={`delete-folder-${folder.id}`}
                      >
                        <TrashIcon />
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}

            {files.map((f) => (
              <tr key={f.id} className="hover:bg-gray-50 transition-colors" data-testid={`file-row-${f.id}`}>
                <td className="px-5 py-3">
                  <div className="flex items-center gap-2">
                    <span className="text-gray-400 shrink-0"><FileIcon contentType={f.contentType ?? ''} /></span>
                    <div className="min-w-0">
                      <button
                        onClick={() => setPreviewFile({ fileName: f.fileName ?? '', contentType: f.contentType ?? '', url: f.url ?? '' })}
                        className="font-medium text-gray-900 truncate block text-left hover:text-brand-600 transition-colors"
                      >
                        {f.fileName}
                      </button>
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
                    <button
                      onClick={() => setPreviewFile({ fileName: f.fileName ?? '', contentType: f.contentType ?? '', url: f.url ?? '' })}
                      className="p-1.5 text-gray-400 hover:text-brand-600 rounded-md hover:bg-brand-50 transition-colors"
                      title="Forhåndsvis"
                    >
                      <EyeIcon />
                    </button>
                    <a
                      href={f.url || undefined}
                      target="_blank"
                      rel="noopener noreferrer"
                      download={f.fileName}
                      data-testid={`download-${f.id}`}
                      className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                      title="Hent fil"
                    >
                      <DownloadIcon />
                    </a>
                    {isAdmin && (
                      <button
                        data-testid={`delete-${f.id}`}
                        onClick={() => {
                          if (f.id && confirm(`Slet filen "${f.fileName ?? 'fil'}"?`)) {
                            deleteMutation.mutate({ path: { id: f.id } })
                          }
                        }}
                        className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                        title="Slet fil"
                      >
                        <TrashIcon />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}

            {isEmpty && (
              <tr>
                <td colSpan={5} className="px-5 py-12 text-center">
                  <p className="text-gray-400 text-sm">
                    {currentFolderId ? 'Mappen er tom' : 'Ingen filer her endnu'}
                  </p>
                  <p className="text-gray-300 text-xs mt-1">
                    {currentFolderId ? 'Upload filer til denne mappe via "Upload fil"' : 'Klik "Upload fil" for at komme i gang'}
                  </p>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {previewFile && <FilePreviewModal {...previewFile} onClose={() => setPreviewFile(null)} />}

      {showUpload && (
        <UploadModal
          courses={courses ?? []}
          currentFolderId={currentFolderId}
          defaultCourseId={filterCourseId || undefined}
          onClose={() => setShowUpload(false)}
          onUploaded={(courseId) => { setShowUpload(false); setFilterCourseId(courseId) }}
        />
      )}

      {showCreateFolder && (
        <CreateFolderModal
          parentId={currentFolderId}
          courses={courses ?? []}
          defaultCourseId={filterCourseId || undefined}
          onClose={() => setShowCreateFolder(false)}
          onCreated={handleFolderCreated}
        />
      )}
    </div>
  )
}
