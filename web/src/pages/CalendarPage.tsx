import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getApiV1CalendarOptions,
  getApiV1CalendarQueryKey,
  getApiV1CalendarDefaultsOptions,
  postApiV1CalendarMutation,
  putApiV1CalendarByIdMutation,
  deleteApiV1CalendarByIdMutation,
  deleteApiV1CalendarByIdOccurrencesByDateMutation,
  deleteApiV1CalendarByIdFromByDateMutation,
} from '../api/generated/@tanstack/react-query.gen'
import type { CalendarEntryDto, DefaultHolidayDto } from '../api/generated/types.gen'
import { usePageTitle } from '../hooks/usePageTitle'
import { DatePicker } from '../components/DatePicker'
import keycloak from '../auth/keycloak'

const TYPE_LABELS: Record<string, string> = {
  Ferie: 'Ferie',
  Lukkedag: 'Lukkedag',
  Arbejdsdag: 'Arbejdsdag',
  Begivenhed: 'Begivenhed',
}

const TYPE_COLORS: Record<string, string> = {
  Ferie: 'bg-blue-200 text-blue-900',
  Lukkedag: 'bg-red-200 text-red-900',
  Arbejdsdag: 'bg-amber-200 text-amber-900',
  Begivenhed: 'bg-purple-200 text-purple-900',
}

const TYPE_BADGE_COLORS: Record<string, string> = {
  Ferie: 'bg-blue-100 text-blue-800',
  Lukkedag: 'bg-red-100 text-red-800',
  Arbejdsdag: 'bg-amber-100 text-amber-800',
  Begivenhed: 'bg-purple-100 text-purple-800',
}

const MONTH_NAMES = [
  'Januar', 'Februar', 'Marts', 'April', 'Maj', 'Juni',
  'Juli', 'August', 'September', 'Oktober', 'November', 'December',
]

const WEEKDAY_HEADERS = ['Ma', 'Ti', 'On', 'To', 'Fr', 'Lø', 'Sø']

function getSchoolYears(schoolStartYear: number): { startYear: number; endYear: number } {
  return { startYear: schoolStartYear, endYear: schoolStartYear + 1 }
}

function getSchoolYearMonths(schoolStartYear: number): Array<{ year: number; month: number }> {
  const months: Array<{ year: number; month: number }> = []
  for (let m = 8; m <= 12; m++) months.push({ year: schoolStartYear, month: m })
  for (let m = 1; m <= 6; m++) months.push({ year: schoolStartYear + 1, month: m })
  return months
}

function getISOWeek(year: number, month: number, day: number): number {
  const date = new Date(year, month - 1, day)
  const thursday = new Date(date)
  thursday.setDate(date.getDate() - ((date.getDay() + 6) % 7) + 3)
  const firstThursday = new Date(thursday.getFullYear(), 0, 4)
  firstThursday.setDate(firstThursday.getDate() - ((firstThursday.getDay() + 6) % 7) + 3)
  return Math.round((thursday.getTime() - firstThursday.getTime()) / 604800000) + 1
}

function isEntryInSchoolYear(entry: CalendarEntryDto, startYear: number): boolean {
  const schoolStart = new Date(startYear, 7, 1)
  const schoolEnd = new Date(startYear + 1, 6, 31)
  const entryStart = new Date(`${entry.startDate}T00:00:00`)
  return entryStart >= schoolStart && entryStart <= schoolEnd
}

function buildMonthGrid(year: number, month: number): (number | null)[][] {
  const firstDay = new Date(year, month - 1, 1)
  const startOffset = (firstDay.getDay() + 6) % 7
  const daysInMonth = new Date(year, month, 0).getDate()
  const cells: (number | null)[] = [
    ...Array(startOffset).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ]
  const weeks: (number | null)[][] = []
  for (let i = 0; i < cells.length; i += 7) {
    const week = cells.slice(i, i + 7)
    while (week.length < 7) week.push(null)
    weeks.push(week)
  }
  return weeks
}

function getDayEntries(
  year: number,
  month: number,
  day: number,
  entries: CalendarEntryDto[],
): CalendarEntryDto[] {
  const pad = (n: number) => n.toString().padStart(2, '0')
  const dateStr = `${year}-${pad(month)}-${pad(day)}`
  return entries.filter((e) => (e.startDate ?? '') <= dateStr && dateStr <= (e.endDate ?? ''))
}

function formatDateDDMMYYYY(isoDate: string): string {
  const [year, month, day] = isoDate.split('-')
  return `${day}/${month}/${year}`
}

function formatDateRange(startDate: string, endDate: string): string {
  if (startDate === endDate) return formatDateDDMMYYYY(startDate)
  return `${formatDateDDMMYYYY(startDate)} – ${formatDateDDMMYYYY(endDate)}`
}

function toDateString(year: number, month: number, day: number): string {
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${year}-${pad(month)}-${pad(day)}`
}

// ─── DeleteOccurrenceDialog ───────────────────────────────────────────────────

type DeleteMode = 'single' | 'from' | 'all'

interface DeleteOccurrenceDialogProps {
  entry: CalendarEntryDto
  occurrenceDate: string // ISO yyyy-MM-dd of the specific occurrence being deleted
  onClose: () => void
  onDeleted: () => void
}

function DeleteOccurrenceDialog({ entry, occurrenceDate, onClose, onDeleted }: DeleteOccurrenceDialogProps) {
  const qc = useQueryClient()
  const [mode, setMode] = useState<DeleteMode>('single')

  const invalidate = () => qc.invalidateQueries({ queryKey: getApiV1CalendarQueryKey() })

  const deleteSingleMutation = useMutation({
    ...deleteApiV1CalendarByIdOccurrencesByDateMutation(),
    onSuccess: () => { invalidate(); onDeleted() },
  })
  const deleteFromMutation = useMutation({
    ...deleteApiV1CalendarByIdFromByDateMutation(),
    onSuccess: () => { invalidate(); onDeleted() },
  })
  const deleteAllMutation = useMutation({
    ...deleteApiV1CalendarByIdMutation(),
    onSuccess: () => { invalidate(); onDeleted() },
  })

  const isPending = deleteSingleMutation.isPending || deleteFromMutation.isPending || deleteAllMutation.isPending

  function handleConfirm() {
    if (isPending) return
    if (mode === 'single') {
      deleteSingleMutation.mutate({ path: { id: entry.id!, date: occurrenceDate } })
    } else if (mode === 'from') {
      deleteFromMutation.mutate({ path: { id: entry.id!, date: occurrenceDate } })
    } else {
      deleteAllMutation.mutate({ path: { id: entry.id! } })
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">Slet begivenhed</h2>
          <p className="text-sm text-gray-500 mt-1">"{entry.title}" gentages. Hvad vil du slette?</p>
        </div>
        <div className="px-6 py-5 space-y-3">
          {([
            ['single', 'Kun denne begivenhed'],
            ['from', 'Denne og alle efterfølgende'],
            ['all', 'Alle begivenheder i serien'],
          ] as [DeleteMode, string][]).map(([val, label]) => (
            <label key={val} className="flex items-center gap-3 cursor-pointer group">
              <input
                type="radio"
                name="deleteMode"
                value={val}
                checked={mode === val}
                onChange={() => setMode(val)}
                className="accent-brand-600 w-4 h-4"
              />
              <span className="text-sm text-gray-800 group-hover:text-gray-900">{label}</span>
            </label>
          ))}
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900">
            Annuller
          </button>
          <button
            onClick={handleConfirm}
            disabled={isPending}
            className="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isPending ? 'Sletter...' : 'Slet'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── DayPopover ───────────────────────────────────────────────────────────────

interface DayPopoverProps {
  year: number
  month: number
  day: number
  entries: CalendarEntryDto[]
  isAdmin: boolean
  onCreateForDate: (dateStr: string) => void
  onEdit: (entry: CalendarEntryDto) => void
  onDelete: (entry: CalendarEntryDto) => void
  onClose: () => void
}

function DayPopover({ year, month, day, entries, isAdmin, onCreateForDate, onEdit, onDelete, onClose }: DayPopoverProps) {
  const ref = useRef<HTMLDivElement>(null)
  const dateStr = toDateString(year, month, day)
  const dayEntries = getDayEntries(year, month, day, entries)

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose()
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [onClose])

  const formattedDate = new Date(`${dateStr}T00:00:00`).toLocaleDateString('da-DK', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  })

  return (
    <div
      ref={ref}
      className="absolute z-30 top-full left-1/2 -translate-x-1/2 mt-1 bg-white rounded-xl shadow-lg border border-gray-200 w-52 text-left"
      onClick={(e) => e.stopPropagation()}
    >
      <div className="px-3 py-2 border-b border-gray-100">
        <p className="text-xs font-medium text-gray-700 capitalize">{formattedDate}</p>
      </div>
      <div className="px-3 py-2 space-y-1">
        {dayEntries.length === 0 && (
          <p className="text-xs text-gray-400 italic">Ingen begivenheder</p>
        )}
        {dayEntries.map((entry) => (
          <div key={`${entry.id}-${entry.startDate}`} className="flex items-center gap-1.5">
            <span className={`inline-block px-1.5 py-0.5 rounded-full text-xs font-medium ${TYPE_BADGE_COLORS[entry.type ?? ''] ?? 'bg-gray-100 text-gray-700'}`}>
              {TYPE_LABELS[entry.type ?? ''] ?? entry.type}
            </span>
            <span className="text-xs text-gray-800 flex-1 truncate">{entry.title}</span>
            {isAdmin && (
              <div className="flex items-center gap-1 flex-shrink-0">
                <button
                  onClick={() => { onEdit(entry); onClose() }}
                  className="text-gray-400 hover:text-gray-700"
                  title="Rediger"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                  </svg>
                </button>
                <button
                  onClick={() => { onDelete(entry); onClose() }}
                  className="text-gray-400 hover:text-red-600"
                  title="Slet"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    <path d="M10 11v6M14 11v6" />
                    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                  </svg>
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
      {isAdmin && (
        <div className="px-3 py-2 border-t border-gray-100">
          <button
            onClick={() => { onCreateForDate(dateStr); onClose() }}
            className="w-full text-left text-xs text-brand-600 hover:text-brand-800 font-medium"
          >
            + Tilføj begivenhed
          </button>
        </div>
      )}
    </div>
  )
}

// ─── EntryModal ───────────────────────────────────────────────────────────────

interface EntryModalProps {
  initial?: CalendarEntryDto
  defaultDate?: string
  onClose: () => void
  onSaved: () => void
}

function EntryModal({ initial, defaultDate, onClose, onSaved }: EntryModalProps) {
  const qc = useQueryClient()
  const today = new Date()
  const todayStr = toDateString(today.getFullYear(), today.getMonth() + 1, today.getDate())
  const initialDate = defaultDate ?? todayStr

  type EntryType = 'Ferie' | 'Lukkedag' | 'Arbejdsdag' | 'Begivenhed'
  const [type, setType] = useState<EntryType>((initial?.type as EntryType) ?? 'Ferie')
  const [title, setTitle] = useState(initial?.title ?? '')
  const [startDate, setStartDate] = useState(initial?.startDate ?? initialDate)
  const [endDate, setEndDate] = useState(initial?.endDate ?? initialDate)
  const [recurrenceRule, setRecurrenceRule] = useState<string>(initial?.recurrenceRule ?? '')
  const [recurrenceEnd, setRecurrenceEnd] = useState<string>(initial?.recurrenceEnd ?? '')

  const dateError = endDate < startDate ? 'Slutdato skal være efter eller lig startdato' : null

  const createMutation = useMutation({
    ...postApiV1CalendarMutation(),
    onSuccess: () => { qc.invalidateQueries({ queryKey: getApiV1CalendarQueryKey() }); onSaved() },
  })
  const updateMutation = useMutation({
    ...putApiV1CalendarByIdMutation(),
    onSuccess: () => { qc.invalidateQueries({ queryKey: getApiV1CalendarQueryKey() }); onSaved() },
  })
  const mutation = initial ? updateMutation : createMutation

  const isPending = createMutation.isPending || updateMutation.isPending
  const isError = createMutation.isError || updateMutation.isError

  function handleSave() {
    if (!title.trim() || dateError || isPending) return
    const body = {
      title,
      type,
      startDate,
      endDate,
      recurrenceRule: recurrenceRule || null,
      recurrenceEnd: recurrenceRule && recurrenceEnd ? recurrenceEnd : null,
    }
    if (initial) {
      updateMutation.mutate({ path: { id: initial.id! }, body })
    } else {
      createMutation.mutate({ body })
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md" onClick={(e) => e.stopPropagation()}>
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">
            {initial ? 'Rediger begivenhed' : 'Tilføj begivenhed'}
          </h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as EntryType)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            >
              {Object.keys(TYPE_LABELS).map((t) => (
                <option key={t} value={t}>{TYPE_LABELS[t]}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Titel *</label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="fx Efterårsferie"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Startdato</label>
            <DatePicker
              value={startDate}
              onChange={(v) => {
                setStartDate(v)
                if (endDate < v) setEndDate(v)
              }}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Slutdato</label>
            <DatePicker
              value={endDate}
              onChange={setEndDate}
              min={startDate}
            />
            {dateError && <p className="mt-1 text-sm text-red-600">{dateError}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Gentagelse</label>
            <select
              value={recurrenceRule}
              onChange={(e) => setRecurrenceRule(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            >
              <option value="">Ingen gentagelse</option>
              <option value="FREQ=WEEKLY">Ugentlig</option>
              <option value="FREQ=WEEKLY;INTERVAL=2">Hver 2. uge</option>
              <option value="FREQ=MONTHLY">Månedlig</option>
            </select>
          </div>
          {recurrenceRule && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Gentag indtil</label>
              <DatePicker
                value={recurrenceEnd}
                onChange={setRecurrenceEnd}
                min={endDate}
              />
            </div>
          )}
          {isError && (
            <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
          )}
        </div>
        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900">
            Annuller
          </button>
          <button
            onClick={handleSave}
            disabled={!title.trim() || !!dateError || mutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {mutation.isPending ? 'Gemmer...' : 'Gem'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── CalendarPage ─────────────────────────────────────────────────────────────

export default function CalendarPage() {
  usePageTitle('Kalender')
  const qc = useQueryClient()
  const isAdmin = keycloak.hasRealmRole('admin')

  const today = new Date()
  const currentSchoolStartYear = today.getMonth() >= 7 ? today.getFullYear() : today.getFullYear() - 1
  const [schoolStartYear, setSchoolStartYear] = useState(currentSchoolStartYear)
  const { startYear, endYear } = getSchoolYears(schoolStartYear)
  const schoolYearMonths = getSchoolYearMonths(schoolStartYear)

  const nowMonthStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`

  function findCurrentMonthIndex(months: Array<{ year: number; month: number }>) {
    const idx = months.findIndex(
      ({ year, month }) => `${year}-${String(month).padStart(2, '0')}` >= nowMonthStr,
    )
    return idx >= 0 ? idx : 0
  }

  const [carouselIndex, setCarouselIndex] = useState(() => findCurrentMonthIndex(schoolYearMonths))

  useEffect(() => {
    setCarouselIndex(findCurrentMonthIndex(getSchoolYearMonths(schoolStartYear)))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [schoolStartYear])

  const touchStartX = useRef<number | null>(null)

  function handleTouchStart(e: React.TouchEvent) {
    touchStartX.current = e.touches[0].clientX
  }
  function handleTouchEnd(e: React.TouchEvent) {
    if (touchStartX.current === null) return
    const delta = e.changedTouches[0].clientX - touchStartX.current
    if (delta < -40) setCarouselIndex((i) => Math.min(i + 1, schoolYearMonths.length - 1))
    else if (delta > 40) setCarouselIndex((i) => Math.max(i - 1, 0))
    touchStartX.current = null
  }

  const [exportPending, setExportPending] = useState(false)
  const [exportDone, setExportDone] = useState(false)
  const exportTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    return () => {
      if (exportTimeoutRef.current) clearTimeout(exportTimeoutRef.current)
    }
  }, [])

  async function handleExportIcs() {
    if (exportPending) return
    setExportPending(true)
    setExportDone(false)
    try {
      await keycloak.updateToken(30)
      const res = await fetch('/api/v1/calendar/export.ics', {
        headers: { Authorization: `Bearer ${keycloak.token}` },
      })
      if (!res.ok) throw new Error('Export fejlede')
      const blob = await res.blob()
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'skoleoverblikket-kalender.ics'
      a.style.display = 'none'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
      setExportDone(true)
      if (exportTimeoutRef.current) clearTimeout(exportTimeoutRef.current)
      exportTimeoutRef.current = setTimeout(() => { setExportDone(false); exportTimeoutRef.current = null }, 8000)
    } finally {
      setExportPending(false)
    }
  }

  const [createDate, setCreateDate] = useState<string | null>(null)
  const [editingEntry, setEditingEntry] = useState<CalendarEntryDto | null>(null)
  const [openPopover, setOpenPopover] = useState<string | null>(null)

  const [deleteTarget, setDeleteTarget] = useState<{ entry: CalendarEntryDto; occurrenceDate: string } | null>(null)

  const { data: entriesStartYear = [] } = useQuery({
    ...getApiV1CalendarOptions({ query: { year: startYear } }),
    select: (d) => (d ?? []) as CalendarEntryDto[],
  })
  const { data: entriesEndYear = [] } = useQuery({
    ...getApiV1CalendarOptions({ query: { year: endYear } }),
    select: (d) => (d ?? []) as CalendarEntryDto[],
  })

  const allEntries: CalendarEntryDto[] = [
    ...entriesStartYear,
    ...entriesEndYear.filter((e) => !entriesStartYear.some((s) => s.id === e.id && s.startDate === e.startDate)),
  ]

  const hasEntries = allEntries.some((e) => isEntryInSchoolYear(e, schoolStartYear))

  const { data: defaults = [] } = useQuery({
    ...getApiV1CalendarDefaultsOptions({ query: { year: schoolStartYear } }),
    enabled: isAdmin && !hasEntries,
    select: (d) => (d ?? []) as DefaultHolidayDto[],
  })

  const seedMutation = useMutation({
    mutationFn: (items: DefaultHolidayDto[]) => {
      const { mutationFn } = postApiV1CalendarMutation()
      return Promise.all(items.map((d) => mutationFn!({ body: d }, undefined as never)))
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: getApiV1CalendarQueryKey() })
    },
  })

  const deleteMutation = useMutation({
    ...deleteApiV1CalendarByIdMutation(),
    onSuccess: () => qc.invalidateQueries({ queryKey: getApiV1CalendarQueryKey() }),
  })

  const yearOptions = Array.from({ length: 5 }, (_, i) => currentSchoolStartYear - 2 + i)

  function handleDayClick(year: number, month: number, day: number, isWeekend: boolean) {
    if (isWeekend) return
    const key = `${year}-${month}-${day}`
    setOpenPopover((prev) => (prev === key ? null : key))
  }

  function handleDeleteEntry(entry: CalendarEntryDto) {
    if (entry.recurrenceRule) {
      setDeleteTarget({ entry, occurrenceDate: entry.startDate! })
    } else {
      if (confirm(`Slet "${entry.title}"?`)) deleteMutation.mutate({ path: { id: entry.id! } })
    }
  }

  function renderMonthCard(year: number, month: number, large = false) {
    const weeks = buildMonthGrid(year, month)
    const dayCellClass = large
      ? 'text-base text-center py-2.5 rounded-lg select-none font-medium'
      : 'text-sm text-center py-1 rounded select-none'
    const headerClass = large ? 'text-sm text-center pb-2' : 'text-xs text-center pb-1'
    const weekNumClass = large
      ? 'text-xs text-gray-400 text-right pr-2 leading-none flex items-center justify-end'
      : 'text-xs text-gray-400 text-right pr-1 py-0.5 leading-none flex items-center justify-end'
    const weekNumCol = large ? '2.5rem' : '2rem'

    return (
      <div key={`${year}-${month}`} className={`bg-white rounded-xl border border-gray-200 ${large ? 'p-6' : 'p-5'}`}>
        <p className={`font-display font-semibold text-gray-700 mb-3 ${large ? 'text-xl' : 'text-base'}`}>
          {MONTH_NAMES[month - 1]} {year}
        </p>
        <div className="grid gap-0" style={{ gridTemplateColumns: `${weekNumCol} repeat(7, 1fr)` }}>
          <div />
          {WEEKDAY_HEADERS.map((h, hi) => (
            <div key={h} className={`${headerClass} ${hi >= 5 ? 'text-gray-400' : 'text-gray-600'}`}>{h}</div>
          ))}
          {weeks.map((week, wi) => {
            const firstDay = week.find((d) => d !== null)
            const weekNum = firstDay != null ? getISOWeek(year, month, firstDay) : null
            return [
              <div key={`wn-${wi}`} className={weekNumClass}>
                {weekNum}
              </div>,
              ...week.map((day, di) => {
                if (day === null) {
                  return <div key={`${wi}-${di}`} className={di >= 5 ? 'bg-gray-100 rounded' : ''} />
                }
                const isWeekend = di >= 5
                const dayEntries = isWeekend ? [] : getDayEntries(year, month, day, allEntries)
                const firstEntry = dayEntries[0]
                const colorClass = firstEntry ? TYPE_COLORS[firstEntry.type ?? ''] ?? '' : ''
                const popoverKey = `${year}-${month}-${day}`
                const isOpen = openPopover === popoverKey

                return (
                  <div key={`${wi}-${di}`} className="relative">
                    <div
                      onClick={() => handleDayClick(year, month, day, isWeekend)}
                      className={[
                        dayCellClass,
                        isWeekend
                          ? 'bg-gray-100 text-gray-400 cursor-default'
                          : colorClass
                            ? `${colorClass} cursor-pointer`
                            : 'text-gray-700 cursor-pointer hover:bg-gray-100',
                      ].join(' ')}
                    >
                      {day}
                    </div>
                    {isOpen && (
                      <DayPopover
                        year={year}
                        month={month}
                        day={day}
                        entries={allEntries}
                        isAdmin={isAdmin}
                        onCreateForDate={(dateStr) => setCreateDate(dateStr)}
                        onEdit={(entry) => setEditingEntry(entry)}
                        onDelete={(entry) => handleDeleteEntry(entry)}
                        onClose={() => setOpenPopover(null)}
                      />
                    )}
                  </div>
                )
              }),
            ]
          })}
        </div>
      </div>
    )
  }

  const currentCarouselMonth = schoolYearMonths[carouselIndex]

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="space-y-3 sm:space-y-0 sm:flex sm:items-center sm:justify-between sm:gap-4">
        <h1 className="font-display text-2xl font-semibold text-gray-900">Kalender</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <select
            value={schoolStartYear}
            onChange={(e) => setSchoolStartYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
          >
            {yearOptions.map((y) => (
              <option key={y} value={y}>{y}/{y + 1}</option>
            ))}
          </select>
          <button
            onClick={handleExportIcs}
            disabled={exportPending}
            title="Åbn filen i Google Calendar, Outlook eller Kalender (iPhone/Mac) for at importere begivenhederne."
            className="border border-gray-300 text-gray-600 hover:bg-gray-50 rounded-lg px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
          >
            {exportPending ? 'Tilføjer...' : 'Tilføj til kalender'}
          </button>
          {isAdmin && !hasEntries && defaults.length > 0 && (
            <button
              onClick={() => seedMutation.mutate(defaults)}
              disabled={seedMutation.isPending}
              className="border border-brand-600 text-brand-600 hover:bg-brand-50 rounded-lg px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
            >
              {seedMutation.isPending ? 'Tilføjer...' : 'Tilføj standardferier'}
            </button>
          )}
        </div>
      </div>

      {/* Export confirmation */}
      {exportDone && (
        <div className="bg-green-50 border border-green-200 rounded-xl px-5 py-3 text-sm text-green-800">
          Filen er hentet. Dobbeltklik på den for at importere – eller åbn din kalender og importer derfra.
        </div>
      )}

      {/* Empty state */}
      {isAdmin && !hasEntries && (
        <div className="bg-brand-50 border border-brand-200 rounded-xl p-5">
          <p className="text-sm text-brand-800 font-medium mb-1">Ingen begivenheder endnu</p>
          <p className="text-sm text-brand-700">
            Tilføj ferier, lukkedage og begivenheder for skoleåret {startYear}/{endYear}. Du kan bruge &quot;Tilføj standardferier&quot; knappen øverst for at komme hurtigt i gang med danske skoleferier.
          </p>
        </div>
      )}

      {/* Mobile carousel — visible below lg */}
      <div className="lg:hidden">
        <div
          onTouchStart={handleTouchStart}
          onTouchEnd={handleTouchEnd}
        >
          {renderMonthCard(currentCarouselMonth.year, currentCarouselMonth.month, true)}
        </div>
        {/* Pagination: arrow ← dots → arrow */}
        <div className="flex items-center justify-center gap-3 mt-4">
          <button
            onClick={() => setCarouselIndex((i) => Math.max(i - 1, 0))}
            disabled={carouselIndex === 0}
            className="p-2 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            aria-label="Forrige måned"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>
          <div className="flex items-center gap-1.5">
            {schoolYearMonths.map((_, i) => (
              <button
                key={i}
                onClick={() => setCarouselIndex(i)}
                aria-label={`Gå til måned ${i + 1}`}
                className={`rounded-full transition-all ${i === carouselIndex ? 'w-4 h-2 bg-brand-600' : 'w-2 h-2 bg-gray-300 hover:bg-gray-400'}`}
              />
            ))}
          </div>
          <button
            onClick={() => setCarouselIndex((i) => Math.min(i + 1, schoolYearMonths.length - 1))}
            disabled={carouselIndex === schoolYearMonths.length - 1}
            className="p-2 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            aria-label="Næste måned"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          </button>
        </div>
      </div>

      {/* Desktop grid — visible from lg */}
      <div className="hidden lg:grid lg:grid-cols-2 xl:grid-cols-3 gap-6">
        {schoolYearMonths.map(({ year, month }) => renderMonthCard(year, month))}
      </div>

      {/* Entry list */}
      {allEntries.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[480px]">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</th>
                <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Titel</th>
                <th className="px-5 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide hidden sm:table-cell">Dato</th>
                {isAdmin && (
                  <th className="px-5 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Handlinger</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {allEntries.map((entry) => (
                <tr key={`${entry.id}-${entry.startDate}`} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3">
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${TYPE_BADGE_COLORS[entry.type ?? ''] ?? 'bg-gray-100 text-gray-700'}`}>
                      {TYPE_LABELS[entry.type ?? ''] ?? entry.type}
                    </span>
                  </td>
                  <td className="px-5 py-3 font-medium text-gray-900">{entry.title}</td>
                  <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">
                    {formatDateRange(entry.startDate ?? '', entry.endDate ?? '')}
                  </td>
                  {isAdmin && (
                    <td className="px-5 py-3 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => setEditingEntry(entry)}
                          className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
                          title="Rediger"
                        >
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                          </svg>
                        </button>
                        <button
                          onClick={() => handleDeleteEntry(entry)}
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
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </div>
      )}

      {createDate !== null && (
        <EntryModal
          defaultDate={createDate}
          onClose={() => setCreateDate(null)}
          onSaved={() => setCreateDate(null)}
        />
      )}
      {editingEntry && (
        <EntryModal
          initial={editingEntry}
          onClose={() => setEditingEntry(null)}
          onSaved={() => setEditingEntry(null)}
        />
      )}
      {deleteTarget && (
        <DeleteOccurrenceDialog
          entry={deleteTarget.entry}
          occurrenceDate={deleteTarget.occurrenceDate}
          onClose={() => setDeleteTarget(null)}
          onDeleted={() => setDeleteTarget(null)}
        />
      )}
    </div>
  )
}
