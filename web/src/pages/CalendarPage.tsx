import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'
import keycloak from '../auth/keycloak'

interface CalendarEntryDto {
  id: string
  type: string
  title: string
  startDate: string
  endDate: string
}

interface DefaultHolidayDto {
  type: string
  title: string
  startDate: string
  endDate: string
}

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

function buildMonthGrid(year: number, month: number): (number | null)[][] {
  // month is 1-based
  const firstDay = new Date(year, month - 1, 1)
  // Monday = 0 ... Sunday = 6
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
  return entries.filter((e) => e.startDate <= dateStr && dateStr <= e.endDate)
}

function formatDateRange(startDate: string, endDate: string): string {
  const fmt = (d: string) => {
    const [, m, day] = d.split('-')
    const monthShort = new Date(`${d}T00:00:00`).toLocaleDateString('da-DK', { month: 'short' })
    return `${parseInt(day)} ${monthShort}`
  }
  if (startDate === endDate) return fmt(startDate)
  return `${fmt(startDate)} – ${fmt(endDate)}`
}

interface EntryModalProps {
  initial?: CalendarEntryDto
  defaultYear: number
  onClose: () => void
  onSaved: () => void
}

function EntryModal({ initial, defaultYear, onClose, onSaved }: EntryModalProps) {
  const qc = useQueryClient()
  const today = new Date()
  const todayStr = `${today.getFullYear()}-${(today.getMonth() + 1).toString().padStart(2, '0')}-${today.getDate().toString().padStart(2, '0')}`

  const [type, setType] = useState(initial?.type ?? 'Ferie')
  const [title, setTitle] = useState(initial?.title ?? '')
  const [startDate, setStartDate] = useState(initial?.startDate ?? todayStr)
  const [endDate, setEndDate] = useState(initial?.endDate ?? todayStr)

  const dateError = endDate < startDate ? 'Slutdato skal være efter eller lig startdato' : null

  const mutation = useMutation({
    mutationFn: () => {
      const body = { title, type, startDate, endDate }
      return initial
        ? api.put(`/calendar/${initial.id}`, body)
        : api.post('/calendar', body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['calendar', defaultYear] })
      onSaved()
    },
  })

  function handleSave() {
    if (!title.trim() || dateError || mutation.isPending) return
    mutation.mutate()
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
              onChange={(e) => setType(e.target.value)}
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
            <input
              type="date"
              value={startDate}
              onChange={(e) => {
                setStartDate(e.target.value)
                if (endDate < e.target.value) setEndDate(e.target.value)
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Slutdato</label>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
            {dateError && <p className="mt-1 text-sm text-red-600">{dateError}</p>}
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

export default function CalendarPage() {
  usePageTitle('Kalender')
  const qc = useQueryClient()
  const isAdmin = keycloak.hasRealmRole('admin')

  const currentYear = new Date().getFullYear()
  const [selectedYear, setSelectedYear] = useState(currentYear)
  const [showCreate, setShowCreate] = useState(false)
  const [editingEntry, setEditingEntry] = useState<CalendarEntryDto | null>(null)

  const { data: entries = [] } = useQuery<CalendarEntryDto[]>({
    queryKey: ['calendar', selectedYear],
    queryFn: () => api.get(`/calendar?year=${selectedYear}`),
  })

  const { data: defaults = [] } = useQuery<DefaultHolidayDto[]>({
    queryKey: ['calendar-defaults', selectedYear],
    queryFn: () => api.get(`/calendar/defaults?year=${selectedYear}`),
    enabled: isAdmin && entries.length === 0,
  })

  const seedMutation = useMutation({
    mutationFn: (items: DefaultHolidayDto[]) =>
      Promise.all(items.map((d) => api.post('/calendar', d))),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['calendar', selectedYear] }),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/calendar/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['calendar', selectedYear] }),
  })

  const yearOptions = [currentYear - 2, currentYear - 1, currentYear, currentYear + 1, currentYear + 2]

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4">
        <h1 className="font-display text-2xl font-semibold text-gray-900">Kalender</h1>
        <div className="flex items-center gap-2 flex-wrap justify-end">
          <select
            value={selectedYear}
            onChange={(e) => setSelectedYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
          >
            {yearOptions.map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
          {isAdmin && entries.length === 0 && defaults.length > 0 && (
            <button
              onClick={() => seedMutation.mutate(defaults)}
              disabled={seedMutation.isPending}
              className="border border-brand-600 text-brand-600 hover:bg-brand-50 rounded-lg px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
            >
              {seedMutation.isPending ? 'Tilføjer...' : 'Tilføj standardferier'}
            </button>
          )}
          {isAdmin && (
            <button
              onClick={() => setShowCreate(true)}
              className="bg-brand-600 text-white rounded-lg px-3 py-1.5 text-sm hover:bg-brand-700 transition-colors"
            >
              Tilføj begivenhed
            </button>
          )}
        </div>
      </div>

      {/* Empty state */}
      {isAdmin && entries.length === 0 && (
        <div className="bg-brand-50 border border-brand-200 rounded-xl p-5">
          <p className="text-sm text-brand-800 font-medium mb-1">Ingen begivenheder endnu</p>
          <p className="text-sm text-brand-700">
            Tilføj ferier, lukkedage og begivenheder for {selectedYear}. Du kan bruge &quot;Tilføj standardferier&quot; for at komme hurtigt i gang med danske skoleferier.
          </p>
          {defaults.length > 0 && (
            <button
              onClick={() => seedMutation.mutate(defaults)}
              disabled={seedMutation.isPending}
              className="mt-3 border border-brand-600 text-brand-600 hover:bg-brand-100 rounded-lg px-3 py-1.5 text-sm disabled:opacity-50 transition-colors"
            >
              {seedMutation.isPending
                ? <span className="flex items-center gap-2"><span className="inline-block w-3 h-3 border-2 border-brand-600 border-t-transparent rounded-full animate-spin" />Tilføjer...</span>
                : 'Tilføj standardferier'}
            </button>
          )}
        </div>
      )}

      {/* 12-month calendar grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => {
          const weeks = buildMonthGrid(selectedYear, month)
          return (
            <div key={month} className="bg-white rounded-xl border border-gray-200 p-4">
              <p className="font-display text-sm font-semibold text-gray-700 mb-3">
                {MONTH_NAMES[month - 1]}
              </p>
              <div className="grid grid-cols-7 gap-0">
                {WEEKDAY_HEADERS.map((h) => (
                  <div key={h} className="text-xs text-gray-400 text-center pb-1">{h}</div>
                ))}
                {weeks.map((week, wi) =>
                  week.map((day, di) => {
                    if (day === null) {
                      return <div key={`${wi}-${di}`} />
                    }
                    const dayEntries = getDayEntries(selectedYear, month, day, entries)
                    const firstEntry = dayEntries[0]
                    const colorClass = firstEntry ? TYPE_COLORS[firstEntry.type] ?? '' : ''
                    const titles = dayEntries.map((e) => e.title).join(', ')
                    return (
                      <div
                        key={`${wi}-${di}`}
                        title={titles || undefined}
                        className={`text-xs text-center py-0.5 rounded ${colorClass || 'text-gray-700'}`}
                      >
                        {day}
                      </div>
                    )
                  })
                )}
              </div>
            </div>
          )
        })}
      </div>

      {/* Entry list */}
      {entries.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
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
              {entries.map((entry) => (
                <tr key={entry.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3">
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${TYPE_BADGE_COLORS[entry.type] ?? 'bg-gray-100 text-gray-700'}`}>
                      {TYPE_LABELS[entry.type] ?? entry.type}
                    </span>
                  </td>
                  <td className="px-5 py-3 font-medium text-gray-900">{entry.title}</td>
                  <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">
                    {formatDateRange(entry.startDate, entry.endDate)}
                  </td>
                  {isAdmin && (
                    <td className="px-5 py-3 text-right">
                      <div className="flex items-center justify-end gap-1">
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
                          onClick={() => {
                            if (confirm(`Slet "${entry.title}"?`)) deleteMutation.mutate(entry.id)
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
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <EntryModal
          defaultYear={selectedYear}
          onClose={() => setShowCreate(false)}
          onSaved={() => setShowCreate(false)}
        />
      )}
      {editingEntry && (
        <EntryModal
          initial={editingEntry}
          defaultYear={selectedYear}
          onClose={() => setEditingEntry(null)}
          onSaved={() => setEditingEntry(null)}
        />
      )}
    </div>
  )
}
