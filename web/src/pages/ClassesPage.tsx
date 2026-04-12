import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, ClassDto, SchemaDto } from '../api/client'
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
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const dateInvalid = !!startDate && !!endDate && startDate > endDate

  const mutation = useMutation({
    mutationFn: async () => {
      const created: SchemaDto = await api.post(`/classes/${classId}/schemas`, { name })
      if ((startDate || endDate) && created.id) {
        await api.put(`/classes/${classId}/schemas/${created.id}/daterange`, {
          startDate: startDate || null,
          endDate: endDate || null,
        })
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['schemas', classId] })
      onSaved()
    },
  })

  function handleSave() {
    if (!name.trim() || mutation.isPending || dateInvalid) return
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
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Startdato</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Slutdato</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                min={startDate || undefined}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              />
            </div>
          </div>
          {dateInvalid && (
            <p className="text-sm text-red-600">Startdato skal være før slutdato.</p>
          )}
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
            disabled={!name.trim() || mutation.isPending || dateInvalid}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Opretter...' : 'Opret'}
          </button>
        </div>
      </div>
    </div>
  )
}

function formatDateRange(startDate?: string | null, endDate?: string | null): string | null {
  if (!startDate && !endDate) return null
  const fmt = (d: string) => {
    const [, m, ] = d.split('-')
    const months = ['jan', 'feb', 'mar', 'apr', 'maj', 'jun', 'jul', 'aug', 'sep', 'okt', 'nov', 'dec']
    return `${months[parseInt(m, 10) - 1]} ${d.slice(0, 4)}`
  }
  if (startDate && endDate) return `${fmt(startDate)} – ${fmt(endDate)}`
  if (startDate) return `fra ${fmt(startDate)}`
  return `til ${fmt(endDate!)}`
}

function isActiveNow(startDate?: string | null, endDate?: string | null): boolean {
  if (!startDate || !endDate) return false
  const today = new Date().toISOString().slice(0, 10)
  return startDate <= today && endDate >= today
}

interface DateRangeModalProps {
  classId: string
  schema: SchemaDto
  onClose: () => void
}

function DateRangeModal({ classId, schema, onClose }: DateRangeModalProps) {
  const qc = useQueryClient()
  const [startDate, setStartDate] = useState(schema.startDate ?? '')
  const [endDate, setEndDate] = useState(schema.endDate ?? '')

  const mutation = useMutation({
    mutationFn: () =>
      api.put(`/classes/${classId}/schemas/${schema.id}/daterange`, {
        startDate: startDate || null,
        endDate: endDate || null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['schemas', classId] })
      onClose()
    },
  })

  function handleSave() {
    if (mutation.isPending) return
    if (startDate && endDate && startDate > endDate) return
    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Sæt datoperiode</h2>
          <p className="text-sm text-gray-500 mt-0.5">{schema.name}</p>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Startdato</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Slutdato</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                min={startDate || undefined}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              />
            </div>
          </div>
          {startDate && endDate && startDate > endDate && (
            <p className="text-sm text-red-600">Startdato skal være før slutdato.</p>
          )}
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
            disabled={mutation.isPending || (!!startDate && !!endDate && startDate > endDate)}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Gemmer...' : 'Gem'}
          </button>
        </div>
      </div>
    </div>
  )
}

function SchemaList({ classId, autoOpenCreate, onAutoOpenHandled }: { classId: string; autoOpenCreate?: boolean; onAutoOpenHandled?: () => void }) {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [copyingSchema, setCopyingSchema] = useState<SchemaDto | null>(null)
  const [editingDateRange, setEditingDateRange] = useState<SchemaDto | null>(null)

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

  const deleteSchemaMutation = useMutation({
    mutationFn: (schemaId: string) => api.delete(`/classes/${classId}/schemas/${schemaId}`),
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
        <div className="flex items-center gap-3">
          <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Skemaer</p>
          <button
            data-testid={`class-ugeplan-${classId}`}
            onClick={() => navigate(`/klasser/${classId}/ugeplan`)}
            className="text-xs font-medium text-brand-600 hover:text-brand-800 transition-colors"
          >
            Ugeplan
          </button>
        </div>
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
        {schemas?.map((s) => {
          const active = isActiveNow(s.startDate, s.endDate)
          const dateRange = formatDateRange(s.startDate, s.endDate)
          return (
          <div
            key={s.id}
            onClick={() => navigate(`/klasser/${classId}/skema/${s.id}`)}
            className="flex items-center justify-between px-4 py-3 bg-white rounded-lg border border-gray-200 hover:border-brand-300 hover:bg-brand-50/30 transition-colors group cursor-pointer"
          >
            <div className="flex items-center gap-3 min-w-0">
              <span className="font-medium text-sm text-gray-800 group-hover:text-brand-700 transition-colors truncate">
                {s.name}
              </span>
              {dateRange && (
                <span className="shrink-0 text-xs text-gray-400">{dateRange}</span>
              )}
              {active && (
                <span className="shrink-0 flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                  <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
                  Aktiv nu
                </span>
              )}
            </div>
            <div className="flex items-center gap-1 shrink-0 ml-4">
              <button
                onClick={(e) => { e.stopPropagation(); setEditingDateRange(s) }}
                className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                title="Sæt datoperiode"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                  <line x1="16" y1="2" x2="16" y2="6" />
                  <line x1="8" y1="2" x2="8" y2="6" />
                  <line x1="3" y1="10" x2="21" y2="10" />
                </svg>
              </button>
              <button
                onClick={(e) => { e.stopPropagation(); setCopyingSchema(s) }}
                className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                title="Kopiér skema"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                </svg>
              </button>
              <button
                onClick={(e) => {
                  e.stopPropagation()
                  if (confirm(`Slet skemaet "${s.name}"? Alle lektioner i skemaet slettes også.`)) {
                    deleteSchemaMutation.mutate(s.id!)
                  }
                }}
                className="p-1.5 text-gray-400 hover:text-red-600 rounded-md hover:bg-red-50 transition-colors"
                title="Slet skema"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polyline points="3 6 5 6 21 6" />
                  <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                  <path d="M10 11v6M14 11v6" />
                  <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                </svg>
              </button>
            </div>
          </div>
          )
        })}
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
      {editingDateRange && (
        <DateRangeModal
          classId={classId}
          schema={editingDateRange}
          onClose={() => setEditingDateRange(null)}
        />
      )}
    </div>
  )
}

export default function ClassesPage() {
  usePageTitle('Klasser')
  const [searchParams, setSearchParams] = useSearchParams()
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [editingClass, setEditingClass] = useState<ClassDto | null>(null)
  const [expandedClass, setExpandedClass] = useState<string | null>(null)
  const [newSchemaForClass, setNewSchemaForClass] = useState<string | null>(null)
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)

  useEffect(() => {
    if (!openMenuId) return
    const close = () => setOpenMenuId(null)
    document.addEventListener('click', close)
    return () => document.removeEventListener('click', close)
  }, [openMenuId])

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
      <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
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
              <div className="relative shrink-0 ml-4" onClick={(e) => e.stopPropagation()}>
                <button
                  data-testid={`class-menu-${cls.id}`}
                  onClick={() => setOpenMenuId(openMenuId === cls.id ? null : cls.id!)}
                  className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                  title="Flere handlinger"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                    <circle cx="5" cy="12" r="1.5" /><circle cx="12" cy="12" r="1.5" /><circle cx="19" cy="12" r="1.5" />
                  </svg>
                </button>
                {openMenuId === cls.id && (
                  <div className="absolute right-0 top-full mt-1 w-36 bg-white border border-gray-200 rounded-lg shadow-lg z-10 py-1">
                    <button
                      data-testid={`class-edit-${cls.id}`}
                      onClick={() => { setEditingClass(cls); setOpenMenuId(null) }}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors"
                    >
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                      </svg>
                      Rediger
                    </button>
                    <button
                      data-testid={`class-delete-${cls.id}`}
                      onClick={() => {
                        setOpenMenuId(null)
                        if (confirm(`Slet klassen "${cls.name}"?`)) deleteMutation.mutate(cls.id!)
                      }}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
                    >
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6M14 11v6" />
                        <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                      </svg>
                      Slet
                    </button>
                  </div>
                )}
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
