import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getApiV1SfoShiftsOptions,
  getApiV1SfoShiftsQueryKey,
  postApiV1SfoShiftsMutation,
  putApiV1SfoShiftsByIdMutation,
  deleteApiV1SfoShiftsByIdMutation,
  postApiV1SfoShiftsByIdStaffByStaffIdMutation,
  deleteApiV1SfoShiftsByIdStaffByStaffIdMutation,
  getApiV1StaffOptions,
  getApiV1SfoUgeplanOptions,
  getApiV1SfoUgeplanQueryKey,
  putApiV1SfoUgeplanShiftsMutation,
} from '../api/generated/@tanstack/react-query.gen'
import type { SfoShiftDto, SfoWeekPlanShiftDto } from '../api/generated/types.gen'
import { usePageTitle } from '../hooks/usePageTitle'

function getISOWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7)
}

function getISOWeekYear(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  return d.getUTCFullYear()
}

function getISOWeeksInYear(year: number): number {
  const dec28 = new Date(Date.UTC(year, 11, 28))
  return getISOWeek(dec28)
}

const DAY_NAMES = ['', 'Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']

interface ShiftForm {
  dayOfWeek: number
  startTime: string
  endTime: string
  label: string
}

const emptyForm = (): ShiftForm => ({ dayOfWeek: 1, startTime: '06:30', endTime: '08:00', label: '' })

export default function SfoPage() {
  usePageTitle('SFO vagtplan')
  const qc = useQueryClient()

  const { data: shifts, isLoading } = useQuery(getApiV1SfoShiftsOptions())
  const { data: staff } = useQuery(getApiV1StaffOptions())

  const [showForm, setShowForm] = useState(false)
  const [editingShift, setEditingShift] = useState<SfoShiftDto | null>(null)
  const [form, setForm] = useState<ShiftForm>(emptyForm())
  const [allDays, setAllDays] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [isoYear, setIsoYear] = useState(() => getISOWeekYear(new Date()))
  const [isoWeek, setIsoWeek] = useState(() => getISOWeek(new Date()))
  const [editingWeekShift, setEditingWeekShift] = useState<SfoWeekPlanShiftDto | null>(null)

  const { data: weekPlan } = useQuery(getApiV1SfoUgeplanOptions({ query: { isoYear, isoWeek } }))

  const upsertBeskrivelseMutation = useMutation({
    ...putApiV1SfoUgeplanShiftsMutation(),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: getApiV1SfoUgeplanQueryKey({ query: { isoYear, isoWeek } }) })
      setEditingWeekShift(null)
    },
  })

  function prevWeek() {
    if (isoWeek === 1) { setIsoYear(y => y - 1); setIsoWeek(getISOWeeksInYear(isoYear - 1)) }
    else setIsoWeek(w => w - 1)
  }
  function nextWeek() {
    const weeksInYear = getISOWeeksInYear(isoYear)
    if (isoWeek === weeksInYear) { setIsoYear(y => y + 1); setIsoWeek(1) }
    else setIsoWeek(w => w + 1)
  }
  function goToThisWeek() {
    setIsoYear(getISOWeekYear(new Date()))
    setIsoWeek(getISOWeek(new Date()))
  }

  const invalidate = () => qc.invalidateQueries({ queryKey: getApiV1SfoShiftsQueryKey() })

  const createMutation = useMutation({
    ...postApiV1SfoShiftsMutation(),
    onSuccess: () => { invalidate(); setShowForm(false); setForm(emptyForm()) },
    onError: () => setFormError('Vagten kunne ikke oprettes.'),
  })

  const updateMutation = useMutation({
    ...putApiV1SfoShiftsByIdMutation(),
    onSuccess: () => { invalidate(); setEditingShift(null) },
    onError: () => setFormError('Vagten kunne ikke opdateres.'),
  })

  const deleteMutation = useMutation({
    ...deleteApiV1SfoShiftsByIdMutation(),
    onSuccess: () => invalidate(),
  })

  const assignStaffMutation = useMutation({
    ...postApiV1SfoShiftsByIdStaffByStaffIdMutation(),
    onSuccess: () => invalidate(),
  })

  const removeStaffMutation = useMutation({
    ...deleteApiV1SfoShiftsByIdStaffByStaffIdMutation(),
    onSuccess: () => invalidate(),
  })

  function openCreate() {
    setEditingShift(null)
    setForm(emptyForm())
    setAllDays(false)
    setFormError(null)
    setShowForm(true)
  }

  function openEdit(shift: SfoShiftDto) {
    setEditingShift(shift)
    setForm({
      dayOfWeek: shift.dayOfWeek ?? 1,
      startTime: shift.startTime ?? '06:30',
      endTime: shift.endTime ?? '08:00',
      label: shift.label ?? '',
    })
    setFormError(null)
    setShowForm(true)
  }

  async function handleSave() {
    const baseBody = {
      startTime: form.startTime,
      endTime: form.endTime,
      label: form.label || null,
    }
    if (editingShift?.id) {
      updateMutation.mutate({ path: { id: editingShift.id }, body: { ...baseBody, dayOfWeek: form.dayOfWeek } })
      return
    }
    if (allDays) {
      try {
        await Promise.all(
          [1, 2, 3, 4, 5].map(day => createMutation.mutateAsync({ body: { ...baseBody, dayOfWeek: day } }))
        )
        invalidate()
        setShowForm(false)
        setForm(emptyForm())
      } catch {
        setFormError('En eller flere vagter kunne ikke oprettes.')
      }
    } else {
      createMutation.mutate({ body: { ...baseBody, dayOfWeek: form.dayOfWeek } })
    }
  }

  // Group by day
  const byDay = new Map<number, SfoShiftDto[]>()
  for (const shift of shifts ?? []) {
    const d = shift.dayOfWeek ?? 1
    if (!byDay.has(d)) byDay.set(d, [])
    byDay.get(d)!.push(shift)
  }

  if (isLoading) {
    return (
      <div className="p-6 lg:p-8 max-w-3xl mx-auto space-y-6">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-64 bg-gray-100 rounded-xl animate-pulse" />
      </div>
    )
  }

  return (
    <div className="p-6 lg:p-8 max-w-3xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="font-display text-2xl font-semibold text-gray-900">SFO Ugeplan</h1>
            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-brand-100 text-brand-700">Ugeplan</span>
          </div>
          <p className="mt-1 text-sm text-gray-500">Tildel medarbejdere til SFO-vagtblokke pr. ugedag.</p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to={`/udskriv/sfo?isoYear=${isoYear}&isoWeek=${isoWeek}`}
            target="_blank"
            className="flex items-center gap-1.5 px-3 py-2 text-sm border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="6 9 6 2 18 2 18 9" />
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
              <rect x="6" y="14" width="12" height="8" />
            </svg>
            Udskriv
          </Link>
          <button
            onClick={openCreate}
            className="flex items-center gap-1.5 px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
          >
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            Ny vagt
          </button>
        </div>
      </div>

      {(shifts ?? []).length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center">
          <p className="text-sm text-gray-500">Ingen vagtblokke oprettet endnu.</p>
          <button
            onClick={openCreate}
            className="mt-3 px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
          >
            Opret første vagt
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {[1, 2, 3, 4, 5].map(day => {
            const dayShifts = byDay.get(day) ?? []
            if (dayShifts.length === 0) return null
            return (
              <div key={day} className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
                <div className="px-4 py-3">
                  <h2 className="text-sm font-semibold text-gray-700">{DAY_NAMES[day]}</h2>
                </div>
                {dayShifts.map(shift => (
                  <ShiftRow
                    key={shift.id}
                    shift={shift}
                    staff={staff ?? []}
                    onEdit={() => openEdit(shift)}
                    onDelete={() => deleteMutation.mutate({ path: { id: shift.id! } })}
                    onAssignStaff={(staffId) => assignStaffMutation.mutate({ path: { id: shift.id!, staffId } })}
                    onRemoveStaff={(staffId) => removeStaffMutation.mutate({ path: { id: shift.id!, staffId } })}
                  />
                ))}
              </div>
            )
          })}
        </div>
      )}

      {/* Per-week activities */}
      {(shifts ?? []).length > 0 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="font-display text-base font-semibold text-gray-800">Aktiviteter denne uge</h2>
            <div className="flex items-center gap-1">
              <button
                onClick={prevWeek}
                className="p-1.5 rounded-md text-gray-500 hover:bg-gray-100 transition-colors"
                title="Forrige uge"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="15 18 9 12 15 6" /></svg>
              </button>
              <button
                onClick={goToThisWeek}
                className="px-2.5 py-1 text-xs font-medium text-gray-600 hover:bg-gray-100 rounded-md transition-colors tabular-nums"
              >
                Uge {isoWeek}, {isoYear}
              </button>
              <button
                onClick={nextWeek}
                className="p-1.5 rounded-md text-gray-500 hover:bg-gray-100 transition-colors"
                title="Næste uge"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="9 18 15 12 9 6" /></svg>
              </button>
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
            {[1, 2, 3, 4, 5].map(day => {
              const dayShifts = byDay.get(day) ?? []
              if (dayShifts.length === 0) return null
              return (
                <div key={day}>
                  <div className="px-4 py-2 bg-gray-50">
                    <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">{DAY_NAMES[day]}</span>
                  </div>
                  {dayShifts.map(shift => {
                    const weekShift = weekPlan?.shifts?.find(ws => ws.sfoShiftId === shift.id)
                    return (
                      <div
                        key={shift.id}
                        className="px-4 py-3 flex items-start justify-between gap-3 hover:bg-gray-50 cursor-pointer group"
                        onClick={() => setEditingWeekShift({ sfoShiftId: shift.id, dayOfWeek: shift.dayOfWeek, startTime: shift.startTime, endTime: shift.endTime, label: shift.label, staff: (shift.staff ?? []) as SfoWeekPlanShiftDto['staff'], beskrivelse: weekShift?.beskrivelse ?? null, id: weekShift?.id ?? '' })}
                      >
                        <div className="min-w-0">
                          <span className="text-sm text-gray-700 font-medium">{shift.startTime} – {shift.endTime}</span>
                          {shift.label && <span className="ml-2 text-xs text-gray-400">{shift.label}</span>}
                          {weekShift?.beskrivelse ? (
                            <p className="mt-0.5 text-sm text-gray-600 line-clamp-2">{weekShift.beskrivelse}</p>
                          ) : (
                            <p className="mt-0.5 text-xs text-gray-400 italic group-hover:text-brand-500">Klik for at tilføje aktivitet…</p>
                          )}
                        </div>
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="shrink-0 text-gray-300 group-hover:text-brand-500 mt-0.5">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                        </svg>
                      </div>
                    )
                  })}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {/* Beskrivelse edit modal */}
      {editingWeekShift && (
        <BeskrivelsModal
          shift={editingWeekShift}
          isoYear={isoYear}
          isoWeek={isoWeek}
          onSave={(beskrivelse) => upsertBeskrivelseMutation.mutate({ body: { isoYear, isoWeek, sfoShiftId: editingWeekShift.sfoShiftId, beskrivelse } })}
          onClose={() => setEditingWeekShift(null)}
          isPending={upsertBeskrivelseMutation.isPending}
        />
      )}

      {/* Shift form modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowForm(false)} />
          <div className="relative bg-white rounded-xl shadow-xl max-w-sm w-full mx-4 p-6 space-y-4">
            <h2 className="text-base font-semibold text-gray-900">
              {editingShift ? 'Rediger vagt' : 'Ny vagtblok'}
            </h2>

            {!editingShift && (
              <label className="flex items-center gap-2 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={allDays}
                  onChange={e => setAllDays(e.target.checked)}
                  className="w-4 h-4 rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                />
                <span className="text-sm font-medium text-gray-700">Alle 5 dage</span>
              </label>
            )}

            {!allDays && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Ugedag</label>
                <select
                  value={form.dayOfWeek}
                  onChange={e => setForm(f => ({ ...f, dayOfWeek: Number(e.target.value) }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                >
                  {[1, 2, 3, 4, 5].map(d => (
                    <option key={d} value={d}>{DAY_NAMES[d]}</option>
                  ))}
                </select>
              </div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Start</label>
                <input
                  type="time"
                  lang="da"
                  value={form.startTime}
                  onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Slut</label>
                <input
                  type="time"
                  lang="da"
                  value={form.endTime}
                  onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Betegnelse (valgfri)</label>
              <input
                value={form.label}
                onChange={e => setForm(f => ({ ...f, label: e.target.value }))}
                placeholder="f.eks. Morgenvagt"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex justify-end gap-3 pt-1">
              <button
                onClick={() => setShowForm(false)}
                className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
              >
                Annuller
              </button>
              <button
                onClick={handleSave}
                disabled={createMutation.isPending || updateMutation.isPending}
                className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
              >
                {createMutation.isPending || updateMutation.isPending ? 'Gemmer...' : 'Gem'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function BeskrivelsModal({ shift, isoYear, isoWeek, onSave, onClose, isPending }: {
  shift: SfoWeekPlanShiftDto
  isoYear: number
  isoWeek: number
  onSave: (beskrivelse: string | null) => void
  onClose: () => void
  isPending: boolean
}) {
  const [text, setText] = useState(shift.beskrivelse ?? '')

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Escape') onClose()
    if ((e.ctrlKey || e.metaKey) && e.key === 's') { e.preventDefault(); onSave(text || null) }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6 space-y-4">
        <div>
          <h2 className="text-base font-semibold text-gray-900">
            {shift.startTime} – {shift.endTime}{shift.label ? ` · ${shift.label}` : ''}
          </h2>
          <p className="text-xs text-gray-400 mt-0.5">Uge {isoWeek}, {isoYear}</p>
        </div>
        <textarea
          autoFocus
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Beskriv aktiviteter for denne vagt denne uge…"
          rows={5}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
        />
        <p className="text-xs text-gray-400">Ctrl+S for at gemme · Esc for at lukke</p>
        <div className="flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
            Annuller
          </button>
          <button
            onClick={() => onSave(text || null)}
            disabled={isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
          >
            {isPending ? 'Gemmer...' : 'Gem'}
          </button>
        </div>
      </div>
    </div>
  )
}

interface ShiftRowProps {
  shift: SfoShiftDto
  staff: { id?: string; name?: string | null }[]
  onEdit: () => void
  onDelete: () => void
  onAssignStaff: (staffId: string) => void
  onRemoveStaff: (staffId: string) => void
}

function ShiftRow({ shift, staff, onEdit, onDelete, onAssignStaff, onRemoveStaff }: ShiftRowProps) {
  const [showStaffPicker, setShowStaffPicker] = useState(false)
  const assignedIds = new Set((shift.staff ?? []).map(s => s.id))
  return (
    <div className="px-4 py-3 space-y-2">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="text-sm font-medium text-gray-800">
            {shift.startTime} – {shift.endTime}
          </span>
          {shift.label && (
            <span className="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded-full">{shift.label}</span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={onEdit}
            className="p-1 text-gray-400 hover:text-brand-600 rounded-md hover:bg-brand-50 transition-colors"
            title="Rediger"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
            </svg>
          </button>
          <button
            onClick={onDelete}
            className="p-1 text-gray-400 hover:text-red-500 rounded-md hover:bg-red-50 transition-colors"
            title="Slet"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="3 6 5 6 21 6" /><path d="M19 6l-1 14H6L5 6" />
              <path d="M10 11v6" /><path d="M14 11v6" /><path d="M9 6V4h6v2" />
            </svg>
          </button>
        </div>
      </div>

      {/* Staff assignment */}
      <div className="flex flex-wrap gap-1.5 items-center">
        {(shift.staff ?? []).map(s => (
          <span
            key={s.id}
            className="inline-flex items-center gap-1 px-2 py-0.5 text-xs bg-brand-50 text-brand-700 rounded-full"
          >
            {s.name}
            <button
              onClick={() => onRemoveStaff(s.id!)}
              className="text-brand-400 hover:text-brand-700"
            >
              <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </span>
        ))}
        {staff.length > 0 && (
          <div className="relative">
            <button
              onClick={() => setShowStaffPicker(p => !p)}
              className="inline-flex items-center gap-1 px-2 py-0.5 text-xs border border-dashed border-gray-300 text-gray-500 rounded-full hover:border-brand-400 hover:text-brand-600 transition-colors"
            >
              <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" />
              </svg>
              Medarbejdere
            </button>
            {showStaffPicker && (
              <>
                <div className="fixed inset-0 z-10" onClick={() => setShowStaffPicker(false)} />
                <div className="absolute left-0 top-7 z-20 bg-white border border-gray-200 rounded-lg shadow-lg py-1 min-w-[200px]">
                  {staff.map(s => {
                    const assigned = assignedIds.has(s.id)
                    return (
                      <button
                        key={s.id}
                        onClick={() => assigned ? onRemoveStaff(s.id!) : onAssignStaff(s.id!)}
                        className="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-50 text-left"
                      >
                        <span className={`w-4 h-4 rounded border flex items-center justify-center shrink-0 ${assigned ? 'bg-brand-600 border-brand-600' : 'border-gray-300'}`}>
                          {assigned && (
                            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="3">
                              <polyline points="20 6 9 17 4 12" />
                            </svg>
                          )}
                        </span>
                        {s.name}
                      </button>
                    )
                  })}
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
