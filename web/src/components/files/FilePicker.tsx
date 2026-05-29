import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getApiV1FilesOptions } from '../../api/generated/@tanstack/react-query.gen'
import { FileIcon } from './fileIcons'
import { formatBytes } from './fileHelpers'

export interface FilePickerProps {
  /** IDs of files currently attached to the slot */
  selectedFileIds: string[]
  /** Called when a file is checked or unchecked */
  onToggle: (fileId: string, checked: boolean) => void
  /** Disabled while a mutation is in flight */
  disabled?: boolean
}

/**
 * Compact flat file list for use inside modals (e.g. ugeplan slot editor).
 * Shows all school files with a search box. No folder navigation — Hanne and
 * Thomas need to tick a file, not manage the file system.
 */
export function FilePicker({ selectedFileIds, onToggle, disabled = false }: FilePickerProps) {
  const [search, setSearch] = useState('')

  const { data, isLoading } = useQuery(getApiV1FilesOptions())
  const allFiles = data?.files ?? []

  const filtered = search.trim()
    ? allFiles.filter(
        (f) =>
          f.fileName?.toLowerCase().includes(search.trim().toLowerCase()) ||
          f.courseName?.toLowerCase().includes(search.trim().toLowerCase())
      )
    : allFiles

  if (isLoading) {
    return (
      <div className="space-y-1 border border-gray-200 rounded-lg p-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="animate-pulse h-8 bg-gray-100 rounded" />
        ))}
      </div>
    )
  }

  if (allFiles.length === 0) {
    return (
      <p className="text-xs text-gray-400 py-2">
        Ingen filer tilgængelige — gå til{' '}
        <a
          href="/filer"
          target="_blank"
          rel="noopener"
          className="text-brand-600 hover:text-brand-800 underline"
        >
          Filer
        </a>{' '}
        for at uploade.
      </p>
    )
  }

  return (
    <div className="space-y-2">
      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Søg efter filnavn eller fag…"
        className="w-full px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
      />

      {filtered.length === 0 ? (
        <p className="text-xs text-gray-400 py-2 text-center">Ingen filer matcher søgningen.</p>
      ) : (
        <div className="max-h-48 overflow-y-auto border border-gray-200 rounded-lg divide-y divide-gray-50">
          {filtered.map((f) => {
            const isChecked = selectedFileIds.includes(f.id!)
            return (
              <label
                key={f.id}
                className="flex items-center gap-2 px-3 py-2 hover:bg-gray-50 cursor-pointer"
              >
                <input
                  type="checkbox"
                  checked={isChecked}
                  disabled={disabled}
                  onChange={(e) => onToggle(f.id!, e.target.checked)}
                  className="rounded border-gray-300 text-brand-600 focus:ring-brand-500 shrink-0"
                />
                <span className="text-gray-400 shrink-0">
                  <FileIcon contentType={f.contentType ?? ''} />
                </span>
                <span className="text-sm text-gray-800 truncate flex-1">{f.fileName}</span>
                {f.courseName && (
                  <span className="text-xs text-gray-400 shrink-0 hidden sm:block">
                    {f.courseName}
                  </span>
                )}
                <span className="text-xs text-gray-400 shrink-0">
                  {formatBytes(f.sizeBytes ?? 0)}
                </span>
              </label>
            )
          })}
        </div>
      )}
    </div>
  )
}
