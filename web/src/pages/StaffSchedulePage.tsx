import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api, StaffDto } from '../api/client'

const WEEKDAYS = ['Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']

interface ScheduleSlotDto {
  weekday: number
  startTime: string
  endTime: string
  courseName: string
  className: string
  roomName?: string | null
  aideName?: string | null
  teacherName?: string | null
}

export default function StaffSchedulePage() {
  const { staffId } = useParams<{ staffId: string }>()

  const { data: staff } = useQuery<StaffDto>({
    queryKey: ['staff', staffId],
    queryFn: () => api.get(`/staff/${staffId}`),
    enabled: !!staffId,
  })

  const { data: slots, isLoading, isError } = useQuery<ScheduleSlotDto[]>({
    queryKey: ['staff-schedule', staffId],
    queryFn: () => api.get(`/staff/${staffId}/schedule`),
    enabled: !!staffId,
  })

  // Group slots by weekday, sorted by startTime
  const byDay: Record<number, ScheduleSlotDto[]> = {}
  for (const slot of slots ?? []) {
    if (!byDay[slot.weekday]) byDay[slot.weekday] = []
    byDay[slot.weekday].push(slot)
  }
  
  // Sort each day's slots by startTime
  for (const day in byDay) {
    byDay[parseInt(day)].sort((a, b) => a.startTime.localeCompare(b.startTime))
  }

  return (
    <div className="p-6 lg:p-8 max-w-4xl mx-auto space-y-6">
      <div className="flex items-center gap-3">
        <Link to="/medarbejdere" className="text-gray-400 hover:text-gray-600 transition-colors">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
        </Link>
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">
            {staff ? `${staff.name}s skema` : 'Skema'}
          </h1>
          <p className="mt-1 text-sm text-gray-500">Ugentligt skema — aktive klasser</p>
        </div>
        {staffId && (
          <a
            href={`/udskriv/medarbejder/${staffId}`}
            target="_blank"
            rel="noopener noreferrer"
            className="ml-auto flex items-center gap-2 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="6 9 6 2 18 2 18 9" />
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
              <rect x="6" y="14" width="12" height="8" />
            </svg>
            Udskriv
          </a>
        )}
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente skema</p>
        </div>
      )}

      {isLoading && (
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-white rounded-xl border border-gray-200 p-5 animate-pulse">
              <div className="h-5 w-24 bg-gray-200 rounded mb-3" />
              <div className="space-y-2">
                <div className="h-12 bg-gray-100 rounded-lg" />
                <div className="h-12 bg-gray-100 rounded-lg" />
              </div>
            </div>
          ))}
        </div>
      )}

      {!isLoading && slots?.length === 0 && (
        <div className="bg-white rounded-xl border border-gray-200 p-10 text-center text-gray-400 text-sm">
          Ingen aktive lektioner denne uge
        </div>
      )}

      {!isLoading && [1, 2, 3, 4, 5].map((day) => {
        const daySlots = byDay[day]
        if (!daySlots?.length) return null
        return (
          <div key={day} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-5 py-3 bg-gray-50 border-b border-gray-200">
              <h2 className="text-sm font-semibold text-gray-700">{WEEKDAYS[day - 1]}</h2>
            </div>
            <div className="divide-y divide-gray-100">
              {daySlots.map((slot, i) => (
                <div key={i} className="px-5 py-3 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-4 min-w-0">
                    <span className="shrink-0 text-xs font-medium text-gray-500 tabular-nums w-20">
                      {slot.startTime}–{slot.endTime}
                    </span>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{slot.courseName}</p>
                      <p className="text-xs text-gray-500">{slot.className}</p>
                    </div>
                  </div>
                  <div className="shrink-0 flex items-center gap-3 text-xs text-gray-400">
                    {slot.roomName && (
                      <span className="flex items-center gap-1">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                          <polyline points="9 22 9 12 15 12 15 22" />
                        </svg>
                        {slot.roomName}
                      </span>
                    )}
                    {slot.aideName && (
                      <span className="text-gray-400">{slot.aideName}</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )
      })}
    </div>
  )
}
