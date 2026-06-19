import { useQuery } from '@tanstack/react-query'
import { usePageTitle } from '../hooks/usePageTitle'
import { getApiV1StaaMaalMedCoverageOptions } from '../api/generated/@tanstack/react-query.gen'
import type { StaaMaalMedControllerSubjectCoverageDto } from '../api/generated/types.gen'

const CATEGORY_LABELS: Record<string, string> = {
  Dansk: 'Dansk',
  Matematik: 'Matematik',
  Engelsk: 'Engelsk',
  Kristendomskundskab: 'Kristendom',
  Historie: 'Historie',
  Idraet: 'Idræt',
  Musik: 'Musik',
  Billedkunst: 'Billedkunst',
  HaandvaerkOgDesign: 'Håndværk',
  Naturfag: 'Naturfag',
  Geografi: 'Geografi',
  Biologi: 'Biologi',
  FysikKemi: 'Fysik/Kemi',
  Samfundsfag: 'Samfundsfag',
  Tysk: 'Tysk',
  Fransk: 'Fransk',
  Madkundskab: 'Madkundskab',
}

function StatusDot({ subject }: { subject: StaaMaalMedControllerSubjectCoverageDto }) {
  const status = subject.status ?? 'missing'
  const colors: Record<string, string> = {
    green: 'bg-green-400',
    yellow: 'bg-yellow-400',
    red: 'bg-red-400',
    missing: 'bg-gray-200',
  }
  const statusLabels: Record<string, string> = {
    green: 'Opfyldt',
    yellow: '75–99%',
    red: 'Under 75%',
    missing: 'Ikke planlagt',
  }

  const hoursSuffix =
    status !== 'missing' ? ` · ${subject.annualHours}t / ${subject.vejledendeAnnualHours}t` : ''

  return (
    <span
      className={`inline-block w-3 h-3 rounded-full ${colors[status] ?? 'bg-gray-200'}`}
      title={`${statusLabels[status] ?? status}${hoursSuffix}`}
    />
  )
}

export default function StaaMaalMedPage() {
  usePageTitle('Stå mål med')

  const { data, isLoading, isError } = useQuery(getApiV1StaaMaalMedCoverageOptions())

  const allCategories = [
    ...new Set(
      (data?.classes ?? []).flatMap((c) => (c.subjects ?? []).map((s) => s.category ?? ''))
    ),
  ].sort()

  return (
    <div className="p-6 lg:p-8 max-w-full mx-auto space-y-8">
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">Stå mål med</h1>
        <p className="mt-1 text-sm text-gray-500">
          Faglig dækning pr. klasse baseret på aktive skemaer
        </p>
      </div>

      <div className="flex flex-wrap gap-4 text-sm text-gray-600">
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full bg-green-400" /> Opfyldt
        </span>
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full bg-yellow-400" /> 75–99%
        </span>
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full bg-red-400" /> Under 75%
        </span>
        <span className="flex items-center gap-1.5">
          <span className="inline-block w-3 h-3 rounded-full bg-gray-200" /> Ikke planlagt
        </span>
      </div>

      {isLoading ? (
        <div className="animate-pulse space-y-2">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-10 bg-gray-100 rounded" />
          ))}
        </div>
      ) : isError ? (
        <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700">
          Kunne ikke hente data. Prøv at genindlæse siden.
        </div>
      ) : (data?.classes ?? []).length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          <p>Ingen klasser med klassetrin og aktive skemaer fundet</p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm border-separate border-spacing-0">
            <thead>
              <tr>
                <th className="sticky left-0 bg-white px-4 py-2.5 text-left font-semibold text-gray-700 border-b border-gray-200 whitespace-nowrap">
                  Klasse
                </th>
                {allCategories.map((cat) => (
                  <th
                    key={cat}
                    className="px-3 py-2.5 text-center font-medium text-gray-600 border-b border-gray-200 whitespace-nowrap text-xs"
                  >
                    {CATEGORY_LABELS[cat] ?? cat}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {(data?.classes ?? []).map((cls) => {
                const subjectMap = Object.fromEntries(
                  (cls.subjects ?? []).map((s) => [s.category ?? '', s])
                )
                return (
                  <tr key={cls.classId} className="hover:bg-gray-50">
                    <td className="sticky left-0 bg-white px-4 py-2.5 font-medium text-gray-900 border-b border-gray-100 whitespace-nowrap">
                      {cls.className}
                    </td>
                    {allCategories.map((cat) => {
                      const subject = subjectMap[cat]
                      return (
                        <td key={cat} className="px-3 py-2.5 text-center border-b border-gray-100">
                          {subject ? (
                            <StatusDot subject={subject} />
                          ) : (
                            <span className="text-gray-200">—</span>
                          )}
                        </td>
                      )
                    })}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
