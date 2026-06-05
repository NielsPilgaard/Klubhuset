import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getApiV1CalendarOptions } from '../../api/generated/@tanstack/react-query.gen'
import type { CalendarEntryDto } from '../../api/client'
import { usePageTitle } from '../../hooks/usePageTitle'

const TYPE_LABELS: Record<string, string> = {
  Ferie: 'Ferie',
  Lukkedag: 'Lukkedag',
  Arbejdsdag: 'Arbejdsdag',
  Begivenhed: 'Begivenhed',
}

const TYPE_COLORS: Record<string, string> = {
  Ferie: 'bg-blue-100 text-blue-800',
  Lukkedag: 'bg-red-100 text-red-800',
  Arbejdsdag: 'bg-amber-100 text-amber-800',
  Begivenhed: 'bg-purple-100 text-purple-800',
}

const MONTH_NAMES = [
  'Januar',
  'Februar',
  'Marts',
  'April',
  'Maj',
  'Juni',
  'Juli',
  'August',
  'September',
  'Oktober',
  'November',
  'December',
]

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return d.toLocaleDateString('da-DK', { day: 'numeric', month: 'long' })
}

function currentSchoolStartYear(): number {
  const today = new Date()
  return today.getMonth() >= 7 ? today.getFullYear() : today.getFullYear() - 1
}

export default function ParentCalendarPage() {
  usePageTitle('Kalender')

  const defaultYear = currentSchoolStartYear()
  const [schoolStartYear, setSchoolStartYear] = useState(defaultYear)
  const startYear = schoolStartYear
  const endYear = schoolStartYear + 1

  const { data: rawStart, isLoading: loadingStart } = useQuery(
    getApiV1CalendarOptions({ query: { year: startYear } })
  )
  const { data: rawEnd, isLoading: loadingEnd } = useQuery(
    getApiV1CalendarOptions({ query: { year: endYear } })
  )

  const isLoading = loadingStart || loadingEnd

  const entriesStart: CalendarEntryDto[] = Array.isArray(rawStart)
    ? (rawStart as CalendarEntryDto[])
    : []
  const entriesEnd: CalendarEntryDto[] = Array.isArray(rawEnd) ? (rawEnd as CalendarEntryDto[]) : []

  // Deduplicate by id+startDate, then filter to Aug–Jul school year window
  const schoolStart = new Date(startYear, 7, 1)
  const schoolEnd = new Date(endYear, 6, 31)
  const seen = new Set<string>()
  const entries: CalendarEntryDto[] = [...entriesStart, ...entriesEnd].filter((e) => {
    const key = `${e.id}-${e.startDate}`
    if (seen.has(key)) return false
    seen.add(key)
    if (!e.startDate) return false
    const d = new Date(`${e.startDate}T00:00:00`)
    return d >= schoolStart && d <= schoolEnd
  })

  // Group by {year, month} so Jan 2027 and Jan 2026 are separate
  type MonthKey = string // "YYYY-M"
  const grouped = entries.reduce<Record<MonthKey, CalendarEntryDto[]>>((acc, entry) => {
    if (!entry.startDate) return acc
    const d = new Date(`${entry.startDate}T00:00:00`)
    const key = `${d.getFullYear()}-${d.getMonth()}`
    if (!acc[key]) acc[key] = []
    acc[key].push(entry)
    return acc
  }, {})

  // School year order: Aug–Dec of startYear, then Jan–Jul of endYear
  const schoolYearMonthKeys: MonthKey[] = [
    ...[7, 8, 9, 10, 11].map((m) => `${startYear}-${m}`),
    ...[0, 1, 2, 3, 4, 5, 6].map((m) => `${endYear}-${m}`),
  ].filter((k) => grouped[k])

  const yearOptions = Array.from({ length: 3 }, (_, i) => defaultYear - 1 + i)

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold text-gray-900">
          Kalender {startYear}/{endYear}
        </h1>
        <select
          value={schoolStartYear}
          onChange={(e) => setSchoolStartYear(Number(e.target.value))}
          className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
        >
          {yearOptions.map((y) => (
            <option key={y} value={y}>
              {y}/{y + 1}
            </option>
          ))}
        </select>
      </div>

      {isLoading ? (
        <div className="text-sm text-gray-500">Indlæser kalender...</div>
      ) : schoolYearMonthKeys.length === 0 ? (
        <p className="text-sm text-gray-500">Ingen kalenderbegivenheder endnu.</p>
      ) : (
        schoolYearMonthKeys.map((key) => {
          const [y, m] = key.split('-').map(Number)
          return (
            <div key={key}>
              <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-2">
                {MONTH_NAMES[m]} {y}
              </h2>
              <div className="space-y-1">
                {grouped[key].map((entry) => (
                  <div
                    key={`${entry.id}-${entry.startDate}`}
                    className="flex items-center gap-3 py-2 px-3 bg-white rounded-lg border border-gray-100"
                  >
                    <span
                      className={`text-xs font-medium px-2 py-0.5 rounded-full shrink-0 ${TYPE_COLORS[entry.type ?? ''] ?? 'bg-gray-100 text-gray-700'}`}
                    >
                      {TYPE_LABELS[entry.type ?? ''] ?? entry.type}
                    </span>
                    <span className="text-sm text-gray-900 flex-1">{entry.title}</span>
                    <span className="text-xs text-gray-400 shrink-0">
                      {entry.startDate && formatDate(entry.startDate)}
                      {entry.endDate &&
                        entry.endDate !== entry.startDate &&
                        ` – ${formatDate(entry.endDate)}`}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )
        })
      )}
    </div>
  )
}
