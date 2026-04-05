import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, TimeSlotDto, ClassDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'

export default function ClassTimeSlotsPage() {
  usePageTitle('Lektionsstruktur')
  const { classId } = useParams<{ classId: string }>()
  const qc = useQueryClient()

  const { data: cls } = useQuery<ClassDto[]>({
    queryKey: ['classes'],
    queryFn: () => api.get('/classes'),
    select: (all) => all.filter((c) => c.id === classId),
  })
  const className = cls?.[0]?.name

  const { data: timeSlots, isLoading } = useQuery<TimeSlotDto[]>({
    queryKey: ['time-slots', classId],
    queryFn: () => api.get(`/classes/${classId}/time-slots`),
    enabled: !!classId,
  })

  const isCustom = timeSlots?.some((s) => s.classId != null)

  const resetMutation = useMutation({
    mutationFn: () => api.put<TimeSlotDto[]>(`/classes/${classId}/time-slots`, []),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['time-slots', classId] }),
  })

  const lessonSlots = timeSlots?.filter((s) => !s.isBreak) ?? []
  const breakSlots = timeSlots?.filter((s) => s.isBreak) ?? []

  return (
    <div className="flex flex-col h-full">
      <div className="shrink-0 bg-white border-b border-gray-200 px-4 lg:px-6 py-3 flex items-center justify-between gap-2">
        <div className="flex items-center gap-3 min-w-0">
          <Link to="/klasser" className="text-gray-400 hover:text-gray-600 transition-colors shrink-0">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </Link>
          <h1 className="font-display text-base font-semibold text-gray-900 truncate">
            Lektionsstruktur{className ? ` — ${className}` : ''}
          </h1>
          {isCustom && (
            <span className="shrink-0 px-2 py-0.5 text-xs font-medium rounded-full bg-amber-100 text-amber-700">
              Tilpasset
            </span>
          )}
        </div>
        {isCustom && (
          <button
            onClick={() => resetMutation.mutate()}
            disabled={resetMutation.isPending}
            className="px-3 py-1.5 text-xs font-medium text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
          >
            {resetMutation.isPending ? 'Nulstiller...' : 'Nulstil til skolens standard'}
          </button>
        )}
      </div>

      <div className="flex-1 overflow-auto p-4 lg:p-6">
        {isLoading ? (
          <div className="animate-pulse space-y-2 max-w-sm">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="h-10 bg-gray-100 rounded-lg" />
            ))}
          </div>
        ) : (
          <div className="max-w-sm space-y-1.5">
            {!isCustom && (
              <p className="text-sm text-gray-500 mb-4">
                Denne klasse bruger skolens standard lektionsstruktur. Lektionsstrukturen redigeres under{' '}
                <Link to="/indstillinger" className="text-brand-600 hover:underline">Skoleindstillinger</Link>.
              </p>
            )}

            {(timeSlots ?? []).length === 0 && (
              <div className="py-10 text-center">
                <p className="text-sm text-gray-500">Ingen lektionsstruktur defineret endnu.</p>
                <Link
                  to="/indstillinger"
                  className="inline-block mt-3 px-4 py-2 text-sm font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
                >
                  Opsæt skoledagen
                </Link>
              </div>
            )}

            {(timeSlots ?? []).map((slot, idx) => (
              slot.isBreak ? (
                <div
                  key={slot.id ?? idx}
                  className="flex items-center gap-3 px-3 py-2 bg-gray-50 border border-dashed border-gray-200 rounded-lg"
                >
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-gray-400 shrink-0">
                    <path d="M18 8h1a4 4 0 0 1 0 8h-1" />
                    <path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4V8z" />
                    <line x1="6" y1="1" x2="6" y2="4" />
                    <line x1="10" y1="1" x2="10" y2="4" />
                    <line x1="14" y1="1" x2="14" y2="4" />
                  </svg>
                  <span className="text-xs text-gray-500 tabular-nums">
                    {slot.startTime?.slice(0, 5)}–{slot.endTime?.slice(0, 5)}
                  </span>
                  <span className="text-xs text-gray-400">Pause</span>
                </div>
              ) : (
                <div
                  key={slot.id ?? idx}
                  className="flex items-center gap-3 px-3 py-2.5 bg-white border border-gray-200 rounded-lg"
                >
                  <span className="text-xs font-medium text-gray-400 w-5 text-right shrink-0 tabular-nums">
                    {lessonSlots.findIndex((s) => s.id === slot.id) + 1}.
                  </span>
                  <span className="text-sm tabular-nums text-gray-700">
                    {slot.startTime?.slice(0, 5)}–{slot.endTime?.slice(0, 5)}
                  </span>
                  {slot.label && (
                    <span className="text-xs text-gray-400">{slot.label}</span>
                  )}
                </div>
              )
            ))}

            {(timeSlots ?? []).length > 0 && (
              <p className="text-xs text-gray-400 pt-2">
                {lessonSlots.length} lektioner · {breakSlots.length} pause{breakSlots.length !== 1 ? 'r' : ''}
              </p>
            )}

            {resetMutation.isError && (
              <p className="text-sm text-red-600 mt-2">Der opstod en fejl. Prøv igen.</p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
