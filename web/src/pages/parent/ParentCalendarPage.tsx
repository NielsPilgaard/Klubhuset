import { useQuery } from '@tanstack/react-query'
import { getApiV1CalendarOptions } from '../../api/generated/@tanstack/react-query.gen'
import type { CalendarEntryDto } from '../../api/generated/types.gen'
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
  'Januar', 'Februar', 'Marts', 'April', 'Maj', 'Juni',
  'Juli', 'August', 'September', 'Oktober', 'November', 'December',
]

function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return d.toLocaleDateString('da-DK', { day: 'numeric', month: 'long' })
}

export default function ParentCalendarPage() {
  usePageTitle('Kalender')

  const currentYear = new Date().getFullYear()
  const { data: rawEntries, isLoading, isError } = useQuery({
    ...getApiV1CalendarOptions({ query: { year: currentYear } }),
  })

  const entries: CalendarEntryDto[] = Array.isArray(rawEntries) ? (rawEntries as CalendarEntryDto[]) : []

  if (isLoading) return <div className="p-6 text-sm text-gray-500">Indlæser kalender...</div>
  if (isError) return <div className="p-6 text-sm text-red-600">Kunne ikke hente kalender.</div>

  const grouped = entries.reduce<Record<number, CalendarEntryDto[]>>((acc, entry) => {
    const month = new Date(entry.startDate!).getMonth()
    if (!acc[month]) acc[month] = []
    acc[month].push(entry)
    return acc
  }, {})

  const sortedMonths = Object.keys(grouped).map(Number).sort((a, b) => a - b)

  return (
    <div className="p-4 md:p-6 space-y-6">
      <h1 className="text-xl font-semibold text-gray-900">Kalender {currentYear}/{currentYear + 1}</h1>
      {sortedMonths.length === 0 ? (
        <p className="text-sm text-gray-500">Ingen kalenderbegivenheder endnu.</p>
      ) : (
        sortedMonths.map((month) => (
          <div key={month}>
            <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-2">
              {MONTH_NAMES[month]}
            </h2>
            <div className="space-y-1">
              {grouped[month].map((entry) => (
                <div
                  key={entry.id}
                  className="flex items-center gap-3 py-2 px-3 bg-white rounded-lg border border-gray-100"
                >
                  <span className={`text-xs font-medium px-2 py-0.5 rounded-full shrink-0 ${TYPE_COLORS[entry.type ?? ''] ?? 'bg-gray-100 text-gray-700'}`}>
                    {TYPE_LABELS[entry.type ?? ''] ?? entry.type}
                  </span>
                  <span className="text-sm text-gray-900 flex-1">{entry.title}</span>
                  <span className="text-xs text-gray-400 shrink-0">
                    {formatDate(entry.startDate!)}
                    {entry.endDate !== entry.startDate && ` – ${formatDate(entry.endDate!)}`}
                  </span>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  )
}
