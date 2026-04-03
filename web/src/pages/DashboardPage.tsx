import { useQuery } from '@tanstack/react-query'
import { api, DashboardStats, StaffRole } from '../api/client'

function StatCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 px-6 py-5">
      <p className="text-sm text-gray-500 font-medium">{label}</p>
      <p className="mt-1 text-3xl font-semibold text-gray-900 tabular-nums">{value}</p>
      {sub && <p className="mt-1 text-sm text-gray-400">{sub}</p>}
    </div>
  )
}

function roleLabel(role: StaffRole): string {
  if (role === 'Teacher') return 'Lærer'
  if (role === 'Aide') return 'Pædagog'
  return 'Vikar'
}

function SkeletonCard() {
  return (
    <div className="bg-white rounded-xl border border-gray-200 px-6 py-5 animate-pulse">
      <div className="h-4 w-24 bg-gray-200 rounded mb-3" />
      <div className="h-8 w-16 bg-gray-200 rounded" />
    </div>
  )
}

function SkeletonTable({ rows = 4 }: { rows?: number }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 overflow-hidden animate-pulse">
      <div className="px-5 py-4 border-b border-gray-100">
        <div className="h-5 w-40 bg-gray-200 rounded" />
      </div>
      <div className="divide-y divide-gray-100">
        {Array.from({ length: rows }).map((_, i) => (
          <div key={i} className="px-5 py-3 flex justify-between">
            <div className="h-4 w-32 bg-gray-100 rounded" />
            <div className="h-4 w-12 bg-gray-100 rounded" />
          </div>
        ))}
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const { data, isLoading, isError, refetch } = useQuery<DashboardStats>({
    queryKey: ['stats', 'dashboard'],
    queryFn: () => api.get('/stats/dashboard'),
  })

  if (isError) {
    return (
      <div className="p-8">
        <div className="bg-red-50 border border-red-200 rounded-xl p-6 text-center">
          <p className="text-red-700 font-medium">Kunne ikke hente statistik</p>
          <button
            onClick={() => refetch()}
            className="mt-3 px-4 py-2 text-sm bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors"
          >
            Prøv igen
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-8">
      {/* Header */}
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">Oversigt</h1>
        <p className="mt-1 text-sm text-gray-500">Status og nøgletal for din skole</p>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
        {isLoading ? (
          Array.from({ length: 6 }).map((_, i) => <SkeletonCard key={i} />)
        ) : (
          <>
            <StatCard label="Klasser" value={data!.classCount} />
            <StatCard label="Medarbejdere" value={data!.staffCount} />
            <StatCard label="Fag" value={data!.courseCount} />
            <StatCard label="Lokaler" value={data!.roomCount} />
            <StatCard
              label="Skemaer"
              value={`${data!.schemasComplete} / ${data!.schemasTotal}`}
              sub="færdige"
            />
            <StatCard
              label="Klasser u. skema"
              value={data!.unassignedClasses.length}
              sub={data!.unassignedClasses.length === 0 ? 'Alle klasser har skema' : 'mangler tildeling'}
            />
          </>
        )}
      </div>

      {/* Tables */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Hours per staff */}
        {isLoading ? (
          <SkeletonTable rows={5} />
        ) : (
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-5 py-4 border-b border-gray-100">
              <h2 className="text-sm font-semibold text-gray-700">Timer pr. medarbejder</h2>
            </div>
            {data!.hoursPerStaff.length === 0 ? (
              <p className="px-5 py-6 text-sm text-gray-400 text-center">Ingen data endnu</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-100">
                    <th className="px-5 py-2 text-left text-xs font-medium text-gray-400 uppercase tracking-wide">Navn</th>
                    <th className="px-5 py-2 text-left text-xs font-medium text-gray-400 uppercase tracking-wide">Rolle</th>
                    <th className="px-5 py-2 text-right text-xs font-medium text-gray-400 uppercase tracking-wide">Timer</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {data!.hoursPerStaff.map((s) => (
                    <tr key={s.staffId} className="hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-2.5 font-medium text-gray-800">{s.staffName}</td>
                      <td className="px-5 py-2.5 text-gray-500">{roleLabel(s.role)}</td>
                      <td className="px-5 py-2.5 text-right tabular-nums text-gray-700">{s.hours}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {/* Unassigned classes */}
        {isLoading ? (
          <SkeletonTable rows={5} />
        ) : (
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-5 py-4 border-b border-gray-100">
              <h2 className="text-sm font-semibold text-gray-700">Klasser med uassignerede lektioner</h2>
            </div>
            {data!.unassignedClasses.length === 0 ? (
              <div className="px-5 py-8 text-center">
                <div className="inline-flex items-center justify-center w-10 h-10 rounded-full bg-brand-50 mb-3">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-brand-500">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                </div>
                <p className="text-sm text-gray-500">Alle lektioner er tildelt</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-100">
                    <th className="px-5 py-2 text-left text-xs font-medium text-gray-400 uppercase tracking-wide">Klasse</th>
                    <th className="px-5 py-2 text-right text-xs font-medium text-gray-400 uppercase tracking-wide">Tomme lektioner</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {data!.unassignedClasses.map((c) => (
                    <tr key={c.classId} className="hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-2.5 font-medium text-gray-800">{c.className}</td>
                      <td className="px-5 py-2.5 text-right">
                        <span className="inline-flex items-center justify-center min-w-[2rem] px-2 py-0.5 rounded-full bg-amber-100 text-amber-700 text-xs font-semibold tabular-nums">
                          {c.emptySlots}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
