import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getApiV1SfoShiftsOptions } from '../api/generated/@tanstack/react-query.gen'
import type { SfoShiftDto } from '../api/generated/types.gen'

const WEEKDAYS = ['Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']

function buildTimeAxis(shifts: SfoShiftDto[]): { startTime: string; endTime: string }[] {
  const seen = new Map<string, { startTime: string; endTime: string }>()
  for (const s of shifts) {
    if (!s.startTime || !s.endTime) continue
    const key = `${s.startTime}-${s.endTime}`
    if (!seen.has(key)) seen.set(key, { startTime: s.startTime, endTime: s.endTime })
  }
  return [...seen.values()].sort((a, b) => a.startTime.localeCompare(b.startTime))
}

export default function SfoPrintPage() {
  const { data: rawShifts, isLoading } = useQuery(getApiV1SfoShiftsOptions())
  const shifts: SfoShiftDto[] = (rawShifts ?? []) as SfoShiftDto[]

  useEffect(() => {
    if (!isLoading) {
      const t = setTimeout(() => window.print(), 300)
      return () => clearTimeout(t)
    }
  }, [isLoading])

  if (isLoading) return <div className="p-8 text-gray-400">Henter SFO vagtplan…</div>

  const timeAxis = buildTimeAxis(shifts)

  // shiftMap: startTime → dayOfWeek (1–5) → shifts
  const shiftMap: Record<string, Record<number, SfoShiftDto[]>> = {}
  for (const s of shifts) {
    if (!s.startTime || !s.dayOfWeek) continue
    if (!shiftMap[s.startTime]) shiftMap[s.startTime] = {}
    if (!shiftMap[s.startTime][s.dayOfWeek]) shiftMap[s.startTime][s.dayOfWeek] = []
    shiftMap[s.startTime][s.dayOfWeek].push(s)
  }

  return (
    <div className="print-page">
      <div className="print-header">
        <div>
          <h1 className="print-title">SFO Vagtplan</h1>
          <p className="print-subtitle">Ugentlig oversigt</p>
        </div>
        <p className="print-date">Udskrevet {new Date().toLocaleDateString('da-DK')}</p>
      </div>

      <table className="print-table">
        <thead>
          <tr>
            <th className="print-th print-th-time">Tid</th>
            {WEEKDAYS.map((d) => (
              <th key={d} className="print-th">{d}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {timeAxis.map((ts) => (
            <tr key={`${ts.startTime}-${ts.endTime}`}>
              <td className="print-td print-td-time">
                <div className="print-td-inner">
                  <span className="print-time">{ts.startTime}</span>
                  <span className="print-time-end">{ts.endTime}</span>
                </div>
              </td>
              {[1, 2, 3, 4, 5].map((day) => {
                const dayShifts = shiftMap[ts.startTime]?.[day]
                return (
                  <td key={day} className="print-td">
                    <div className="print-td-inner">
                      {dayShifts?.map((shift) => (
                        <div key={shift.id} className="print-cell">
                          {shift.label && (
                            <span className="print-course">{shift.label}</span>
                          )}
                          {shift.staff?.map((s) => (
                            <span key={s.id} className="print-info">{s.name}</span>
                          ))}
                        </div>
                      ))}
                    </div>
                  </td>
                )
              })}
            </tr>
          ))}
        </tbody>
      </table>

      {shifts.length === 0 && (
        <p className="print-empty">Ingen vagtblokke oprettet endnu</p>
      )}
    </div>
  )
}
