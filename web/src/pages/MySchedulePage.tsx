import { useQuery } from '@tanstack/react-query'
import {
  getApiV1StaffMeOptions,
  getApiV1StaffByStaffIdScheduleOptions,
} from '../api/generated/@tanstack/react-query.gen'
import type { ScheduleSlotDto } from '../api/generated/types.gen'

const WEEKDAYS: { key: string; label: string; num: number }[] = [
  { key: 'Monday',    label: 'Mandag',  num: 1 },
  { key: 'Tuesday',   label: 'Tirsdag', num: 2 },
  { key: 'Wednesday', label: 'Onsdag',  num: 3 },
  { key: 'Thursday',  label: 'Torsdag', num: 4 },
  { key: 'Friday',    label: 'Fredag',  num: 5 },
]

const WEEKDAY_NUM: Record<string, number> = {
  Sunday: 0, Monday: 1, Tuesday: 2, Wednesday: 3, Thursday: 4, Friday: 5, Saturday: 6,
}

function toNum(weekday: string | number | undefined): number | null {
  if (weekday === undefined) return null
  if (typeof weekday === 'number') return weekday
  const n = WEEKDAY_NUM[weekday]
  if (n === undefined) { console.warn(`Unknown weekday: ${weekday}`); return null }
  return n
}

function hexToAlpha(color: string, alpha: string): string {
  const hex = color.startsWith('#') ? color.slice(1) : color
  if (hex.length === 3 || hex.length === 6) return color + alpha
  return '#f3f4f6'
}

function buildTimeAxis(slots: ScheduleSlotDto[]) {
  const seen = new Map<string, { startTime: string; endTime: string; sort: number }>()
  for (const s of slots) {
    if (!s.startTime || !s.endTime) continue
    const key = `${s.startTime}-${s.endTime}`
    if (!seen.has(key)) {
      seen.set(key, { startTime: s.startTime, endTime: s.endTime, sort: parseInt(s.startTime.replace(':', ''), 10) })
    }
  }
  return [...seen.values()].sort((a, b) => a.sort - b.sort)
}

export default function MySchedulePage() {
  const { data: me, isLoading: meLoading, isError: meError } = useQuery(getApiV1StaffMeOptions())

  const { data: rawSlots, isLoading: scheduleLoading, isError: scheduleError } = useQuery({
    ...getApiV1StaffByStaffIdScheduleOptions({ path: { staffId: me?.id ?? '' } }),
    enabled: !!me?.id,
  })

  const slots: ScheduleSlotDto[] = Array.isArray(rawSlots) ? rawSlots as ScheduleSlotDto[] : []
  const timeAxis = buildTimeAxis(slots)

  const slotMap: Record<string, Record<number, ScheduleSlotDto[]>> = {}
  for (const s of slots) {
    const day = toNum(s.weekday)
    if (day === null) continue
    if (!slotMap[s.startTime!]) slotMap[s.startTime!] = {}
    if (!slotMap[s.startTime!][day]) slotMap[s.startTime!][day] = []
    slotMap[s.startTime!][day].push(s)
  }

  const isLoading = meLoading || (!!me && scheduleLoading)
  const isError = meError || scheduleError

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto space-y-6">
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">
          {me ? `${me.name}s skema` : 'Mit skema'}
        </h1>
        <p className="mt-1 text-sm text-gray-500">Ugentligt skema — aktive klasser</p>
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5">
          <p className="text-red-700 text-sm font-medium">Kunne ikke hente skema</p>
        </div>
      )}

      {isLoading && (
        <div className="bg-white rounded-xl border border-gray-200 p-5 animate-pulse space-y-3">
          <div className="h-5 w-48 bg-gray-200 rounded" />
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-12 bg-gray-100 rounded-lg" />
          ))}
        </div>
      )}

      {!isLoading && !isError && slots.length === 0 && (
        <div className="bg-white rounded-xl border border-gray-200 p-10 text-center text-gray-400 text-sm">
          Ingen aktive lektioner denne uge
        </div>
      )}

      {!isLoading && slots.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-x-auto">
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="px-3 py-2.5 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide w-20 border-r border-gray-200">
                  Tid
                </th>
                {WEEKDAYS.map((d) => (
                  <th key={d.key} className="px-3 py-2.5 text-center text-xs font-semibold text-gray-700 uppercase tracking-wide">
                    {d.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {timeAxis.map((ts) => (
                <tr key={ts.startTime} className="hover:bg-gray-50/50">
                  <td className="px-3 py-2 text-right align-top border-r border-gray-200 bg-gray-50/50 whitespace-nowrap">
                    <span className="block text-xs font-semibold text-gray-600">{ts.startTime}</span>
                    <span className="block text-xs text-gray-400">{ts.endTime}</span>
                  </td>
                  {WEEKDAYS.map((d) => {
                    const daySlots = slotMap[ts.startTime]?.[d.num]
                    return (
                      <td key={d.key} className="px-2 py-2 align-top">
                        {daySlots?.map((slot, i) => (
                          <div
                            key={i}
                            className="rounded-lg px-2 py-1.5 mb-1 last:mb-0"
                            style={slot.courseColor ? {
                              backgroundColor: hexToAlpha(slot.courseColor, '22'),
                              borderLeft: `3px solid ${slot.courseColor}`,
                            } : {
                              backgroundColor: '#f3f4f6',
                              borderLeft: '3px solid #d1d5db',
                            }}
                          >
                            <p
                              className="text-xs font-semibold leading-tight truncate"
                              style={slot.courseColor ? { color: slot.courseColor } : { color: '#111827' }}
                            >
                              {slot.courseName}
                            </p>
                            <p className="text-xs text-gray-500 truncate">{slot.className}</p>
                            {slot.roomName && (
                              <p className="text-xs text-gray-400 truncate">{slot.roomName}</p>
                            )}
                          </div>
                        ))}
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
