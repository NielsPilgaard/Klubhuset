import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, ClassDto, SchemaDto, SchemaStatus } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'

interface CopySchemaModalProps {
  classId: string
  schemaId: string
  sourceName: string
  onClose: () => void
  onSaved: () => void
}

function CopySchemaModal({ classId, schemaId, sourceName, onClose, onSaved }: CopySchemaModalProps) {
  const qc = useQueryClient()
  const [name, setName] = useState(`Kopi af ${sourceName}`)
  const [targetClassId, setTargetClassId] = useState(classId)

  const { data: allClasses } = useQuery<ClassDto[]>({
    queryKey: ['classes'],
    queryFn: () => api.get('/classes'),
  })

  const mutation = useMutation({
    mutationFn: () => {
      if (targetClassId === classId) {
        return api.post(`/classes/${classId}/schemas/${schemaId}/copy`, { name })
      }
      return api.post(`/classes/${classId}/schemas/${schemaId}/copy-to/${targetClassId}`, { name })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['schemas', classId] })
      qc.invalidateQueries({ queryKey: ['schemas', targetClassId] })
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
          <h2 className="font-display text-lg font-semibold text-gray-900">Kopiér skema</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Kopiér til klasse</label>
            <select
              value={targetClassId}
              onChange={(e) => setTargetClassId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white"
            >
              {allClasses?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}{c.id === classId ? ' (samme klasse)' : ''}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Navn på kopi *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleSave() } }}
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
            {mutation.isPending ? 'Kopierer...' : 'Kopiér'}
          </button>
        </div>
      </div>
    </div>
  )
}

interface ClassModalProps {
  initial?: ClassDto
  onClose: () => void
  onSaved: () => void
}

function ClassModal({ initial, onClose, onSaved }: ClassModalProps) {
  const qc = useQueryClient()
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')

  const mutation = useMutation({
    mutationFn: () =>
      initial
        ? api.put(`/classes/${initial.id}`, { name, description: description || null })
        : api.post('/classes', { name, description: description || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['classes'] })
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
            {initial ? 'Rediger klasse' : 'Opret klasse'}
          </h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Navn *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleSave() } }}
              placeholder="fx 5.a"
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
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
          >
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

interface SchemaModalProps {
  classId: string
  onClose: () => void
  onSaved: () => void
}

function SchemaModal({ classId, onClose, onSaved }: SchemaModalProps) {
  const qc = useQueryClient()
  const [name, setName] = useState('')

  const mutation = useMutation({
    mutationFn: () => api.post(`/classes/${classId}/schemas`, { name }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['schemas', classId] })
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
          <h2 className="font-display text-lg font-semibold text-gray-900">Opret skema</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Navn *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleSave() } }}
              placeholder="fx Efterår 2025"
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
            {mutation.isPending ? 'Opretter...' : 'Opret'}
          </button>
        </div>
      </div>
    </div>
  )
}

function statusLabel(status: SchemaStatus) {
  return status === 'Complete' ? 'Færdig' : 'Kladde'
}

function statusClasses(status: SchemaStatus) {
  return status === 'Complete'
    ? 'bg-brand-100 text-brand-700'
    : 'bg-amber-100 text-amber-700'
}

function SchemaList({ classId, autoOpenCreate, onAutoOpenHandled }: { classId: string; autoOpenCreate?: boolean; onAutoOpenHandled?: () => void }) {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [copyingSchema, setCopyingSchema] = useState<SchemaDto | null>(null)

  useEffect(() => {
    if (autoOpenCreate) {
      setShowCreate(true)
      onAutoOpenHandled?.()
    }
  }, [autoOpenCreate, onAutoOpenHandled])

  const { data: schemas, isLoading } = useQuery<SchemaDto[]>({
    queryKey: ['schemas', classId],
    queryFn: () => api.get(`/classes/${classId}/schemas`),
  })

  const activateMutation = useMutation({
    mutationFn: (schemaId: string) =>
      api.post(`/classes/${classId}/schemas/${schemaId}/activate`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['schemas', classId] }),
  })

  if (isLoading) {
    return (
      <div className="px-6 py-4 animate-pulse space-y-2">
        {[1, 2].map((i) => (
          <div key={i} className="h-10 bg-gray-100 rounded-lg" />
        ))}
      </div>
    )
  }

  return (
    <div className="border-t border-gray-100 bg-brand-50/40">
      <div className="px-6 py-3 flex items-center justify-between">
        <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Skemaer</p>
        <button
          onClick={() => setShowCreate(true)}
          className="flex items-center gap-1.5 text-xs font-medium text-brand-600 hover:text-brand-800 transition-colors"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nyt skema
        </button>
      </div>

      {schemas && schemas.length === 0 && (
        <p className="px-6 pb-4 text-sm text-gray-400">Ingen skemaer endnu</p>
      )}

      <div className="px-4 pb-4 space-y-2">
        {schemas?.map((s) => (
          <div
            key={s.id}
            className="flex items-center justify-between px-4 py-3 bg-white rounded-lg border border-gray-200 hover:border-brand-300 transition-colors group"
          >
            <div className="flex items-center gap-3 min-w-0">
              <button
                onClick={() => navigate(`/klasser/${classId}/skema/${s.id}`)}
                className="font-medium text-sm text-gray-800 group-hover:text-brand-700 transition-colors truncate"
              >
                {s.name}
              </button>
              <span className={`shrink-0 px-2 py-0.5 rounded-full text-xs font-medium ${statusClasses(s.status)}`}>
                {statusLabel(s.status)}
              </span>
              {s.isActive && (
                <span className="shrink-0 px-2 py-0.5 rounded-full text-xs font-medium bg-brand-600 text-white">
                  Aktiv
                </span>
              )}
            </div>
            <div className="flex items-center gap-2 shrink-0 ml-4">
              <button
                onClick={() => navigate(`/klasser/${classId}/skema/${s.id}`)}
                className="px-3 py-1.5 text-xs font-medium text-brand-600 bg-brand-50 hover:bg-brand-100 rounded-md transition-colors"
              >
                Rediger
              </button>
              <button
                onClick={() => setCopyingSchema(s)}
                className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                title="Kopiér skema"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                </svg>
              </button>
              {!s.isActive && (
                <button
                  onClick={() => activateMutation.mutate(s.id)}
                  disabled={activateMutation.isPending}
                  className="px-3 py-1.5 text-xs font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-md transition-colors disabled:opacity-50"
                >
                  Aktivér
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {showCreate && (
        <SchemaModal
          classId={classId}
          onClose={() => setShowCreate(false)}
          onSaved={() => setShowCreate(false)}
        />
      )}
      {copyingSchema && (
        <CopySchemaModal
          classId={classId}
          schemaId={copyingSchema.id!}
          sourceName={copyingSchema.name!}
          onClose={() => setCopyingSchema(null)}
          onSaved={() => setCopyingSchema(null)}
        />
      )}
    </div>
  )
}

export default function ClassesPage() {
  usePageTitle('Klasser')
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [editingClass, setEditingClass] = useState<ClassDto | null>(null)
  const [expandedClass, setExpandedClass] = useState<string | null>(null)
  const [newSchemaForClass, setNewSchemaForClass] = useState<string | null>(null)

  const { data: classes, isLoading, isError, refetch } = useQuery<ClassDto[]>({
    queryKey: ['classes'],
    queryFn: () => api.get('/classes'),
  })

  useEffect(() => {
    const action = searchParams.get('action')
    const classId = searchParams.get('classId')
    if (action === 'new-schema' && classId && classes) {
      const match = classes.find((c) => c.id === classId)
      if (match) {
        setExpandedClass(classId)
        setNewSchemaForClass(classId)
        setSearchParams({}, { replace: true })
      }
    }
  }, [searchParams, classes, setSearchParams])

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/classes/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['classes'] }),
  })

  const toggleExpand = (id: string) => {
    setExpandedClass((prev) => (prev === id ? null : id))
  }

  return (
    <div className="p-6 lg:p-8 max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Klasser</h1>
          <p className="mt-1 text-sm text-gray-500">Administrer klasser og deres skemaer</p>
        </div>
        <button
          onClick={() => setShowCreate(true)}
          className="flex items-center gap-2 px-3 py-2 sm:px-4 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          <span className="hidden sm:inline">Opret klasse</span>
        </button>
      </div>

      {/* Error */}
      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 flex items-center justify-between">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente klasser</p>
          <button
            onClick={() => refetch()}
            className="text-sm px-3 py-1.5 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors"
          >
            Prøv igen
          </button>
        </div>
      )}

      {/* List */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden divide-y divide-gray-100">
        {isLoading &&
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="px-5 py-4 animate-pulse flex justify-between">
              <div className="h-5 w-20 bg-gray-200 rounded" />
              <div className="h-5 w-32 bg-gray-100 rounded" />
            </div>
          ))}

        {!isLoading && classes?.length === 0 && (
          <div className="px-5 py-10 text-center text-gray-400 text-sm">
            Ingen klasser oprettet endnu
          </div>
        )}

        {classes?.map((cls) => (
          <div key={cls.id}>
            <div
              className="flex items-center justify-between px-5 py-4 cursor-pointer"
              onClick={() => toggleExpand(cls.id!)}
              data-testid={`class-row-${cls.id}`}
            >
              <button
                onClick={(e) => { e.stopPropagation(); toggleExpand(cls.id!) }}
                className="flex items-center gap-3 min-w-0 text-left group"
              >
                <svg
                  width="16" height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  className={`shrink-0 text-gray-400 transition-transform ${expandedClass === cls.id ? 'rotate-90' : ''}`}
                >
                  <polyline points="9 18 15 12 9 6" />
                </svg>
                <span className="font-semibold text-gray-900 group-hover:text-brand-700 transition-colors">
                  {cls.name}
                </span>
                {cls.description && (
                  <span className="text-sm text-gray-400 truncate">{cls.description}</span>
                )}
              </button>
              <div className="flex items-center gap-2 shrink-0 ml-4" onClick={(e) => e.stopPropagation()}>
                <button
                  onClick={() => navigate(`/klasser/${cls.id}/ugeplan`)}
                  className="px-3 py-1.5 text-xs font-medium text-brand-600 bg-brand-50 hover:bg-brand-100 rounded-md transition-colors"
                >
                  Ugeplan
                </button>
                <button
                  onClick={() => setEditingClass(cls)}
                  className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                  title="Rediger"
                >
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                  </svg>
                </button>
                <button
                  onClick={() => {
                    if (confirm(`Slet klassen "${cls.name}"?`)) deleteMutation.mutate(cls.id)
                  }}
                  className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                  title="Slet"
                >
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    <path d="M10 11v6M14 11v6" />
                    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                  </svg>
                </button>
              </div>
            </div>
            {expandedClass === cls.id && (
              <SchemaList
                classId={cls.id}
                autoOpenCreate={newSchemaForClass === cls.id}
                onAutoOpenHandled={() => setNewSchemaForClass(null)}
              />
            )}
          </div>
        ))}
      </div>

      {/* Modals */}
      {showCreate && (
        <ClassModal onClose={() => setShowCreate(false)} onSaved={() => setShowCreate(false)} />
      )}
      {editingClass && (
        <ClassModal
          initial={editingClass}
          onClose={() => setEditingClass(null)}
          onSaved={() => setEditingClass(null)}
        />
      )}
    </div>
  )
}
