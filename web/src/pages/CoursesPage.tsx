import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, CourseDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'

const PRESET_COURSES = [
  'Dansk', 'Matematik', 'Engelsk', 'Naturfag', 'Historie', 'Musik',
  'Idræt', 'Kristendom', 'Billedkunst', 'Håndværk og design',
  'Tysk', 'Fransk', 'Geografi', 'Biologi', 'Fysik/kemi', 'Samfundsfag',
]

interface CourseModalProps {
  initial?: CourseDto
  onClose: () => void
  onSaved: () => void
}

function CourseModal({ initial, onClose, onSaved }: CourseModalProps) {
  const qc = useQueryClient()
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')

  const mutation = useMutation({
    mutationFn: () => {
      const body = { name, description: description || null }
      return initial
        ? api.put(`/courses/${initial.id}`, body)
        : api.post('/courses', body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['courses'] })
      onSaved()
    },
  })

  function handleSave() {
    if (!name.trim() || mutation.isPending) return
    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">
            {initial ? 'Rediger fag' : 'Opret fag'}
          </h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          {!initial && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Vælg fra liste</label>
              <div className="flex flex-wrap gap-1.5">
                {PRESET_COURSES.map((preset) => (
                  <button
                    key={preset}
                    type="button"
                    onClick={() => setName(preset)}
                    className={`px-2.5 py-1 text-xs rounded-full border transition-colors ${
                      name === preset
                        ? 'bg-brand-600 text-white border-brand-600'
                        : 'border-gray-300 text-gray-600 hover:border-brand-400 hover:text-brand-700'
                    }`}
                  >
                    {preset}
                  </button>
                ))}
              </div>
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Navn *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleSave() } }}
              placeholder="fx Matematik"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Beskrivelse</label>
            <input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleSave() } }}
              placeholder="Valgfri beskrivelse"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          {mutation.isError && (
            <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
          )}
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900">
            Annuller
          </button>
          <button
            onClick={handleSave}
            disabled={!name.trim() || mutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Gemmer...' : 'Gem'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default function CoursesPage() {
  usePageTitle('Fag')
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [editingCourse, setEditingCourse] = useState<CourseDto | null>(null)

  const { data: courses, isLoading, isError, refetch } = useQuery<CourseDto[]>({
    queryKey: ['courses'],
    queryFn: () => api.get('/courses'),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/courses/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['courses'] }),
  })

  return (
    <div className="p-6 lg:p-8 max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Fag</h1>
          <p className="mt-1 text-sm text-gray-500">Administrer skolens fag</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Opret fag
        </button>
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 flex items-center justify-between">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente fag</p>
          <button onClick={() => refetch()} className="text-sm px-3 py-1.5 bg-red-100 text-red-700 rounded-lg">
            Prøv igen
          </button>
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Navn</th>
              <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden sm:table-cell">Beskrivelse</th>
              <th className="px-5 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Handlinger</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {isLoading &&
              Array.from({ length: 4 }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  <td className="px-5 py-3"><div className="h-4 w-24 bg-gray-200 rounded" /></td>
                  <td className="px-5 py-3 hidden sm:table-cell"><div className="h-4 w-40 bg-gray-100 rounded" /></td>
                  <td className="px-5 py-3" />
                </tr>
              ))}
            {!isLoading && courses?.length === 0 && (
              <tr>
                <td colSpan={3} className="px-5 py-10 text-center text-gray-400">
                  Ingen fag oprettet endnu
                </td>
              </tr>
            )}
            {courses?.map((c) => (
              <tr key={c.id} className="hover:bg-gray-50 transition-colors">
                <td className="px-5 py-3 font-medium text-gray-900">{c.name}</td>
                <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">{c.description ?? '—'}</td>
                <td className="px-5 py-3 text-right">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      onClick={() => setEditingCourse(c)}
                      className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                    >
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                      </svg>
                    </button>
                    <button
                      onClick={() => {
                        if (confirm(`Slet faget "${c.name}"?`)) deleteMutation.mutate(c.id)
                      }}
                      className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                    >
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6M14 11v6" />
                        <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <CourseModal onClose={() => setShowCreate(false)} onSaved={() => setShowCreate(false)} />
      )}
      {editingCourse && (
        <CourseModal
          initial={editingCourse}
          onClose={() => setEditingCourse(null)}
          onSaved={() => setEditingCourse(null)}
        />
      )}
    </div>
  )
}
