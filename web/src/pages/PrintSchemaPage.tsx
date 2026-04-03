import { useParams, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api, StaffDto, RoomDto, ClassDto } from '../api/client'

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

interface ClassSchemaSlot {
  weekday: number
  startTime: string
  endTime: string
  sortOrder: number
  courseName: string
  teacherName?: string | null
  roomName?: string | null
  aideName?: string | null
}

// Collects unique time labels across all active slots
// Tolerates both ClassSchemaSlot (with sortOrder) and ScheduleSlotDto (without)
function buildTimeAxis(slots: (ClassSchemaSlot | ScheduleSlotDto)[]): { startTime: string; endTime: string; sortOrder: number }[] {
  const seen = new Map<string, { startTime: string; endTime: string; sortOrder: number }>()
  for (const s of slots) {
    const key = `${s.startTime}-${s.endTime}`
    if (!seen.has(key)) {
      // Use sortOrder if available (ClassSchemaSlot), otherwise derive from startTime
      const sortOrder = 'sortOrder' in s ? s.sortOrder : parseInt(s.startTime.replace(':', ''), 10)
      seen.set(key, { startTime: s.startTime, endTime: s.endTime, sortOrder })
    }
  }
  return [...seen.values()].sort((a, b) => a.sortOrder - b.sortOrder)
}

type PrintMode = 'class' | 'staff' | 'room'

function PrintGrid({ title, subtitle, slots }: {
  title: string
  subtitle: string
  slots: ScheduleSlotDto[] | ClassSchemaSlot[]
}) {
  // Build time axis
  const timeAxis = buildTimeAxis(slots)

  // Build slot map: startTime → weekday → slots array (to preserve collisions)
  const slotMap: Record<string, Record<number, Array<ScheduleSlotDto | ClassSchemaSlot>>> = {}
  for (const s of slots) {
    if (!slotMap[s.startTime]) slotMap[s.startTime] = {}
    if (!slotMap[s.startTime][s.weekday]) slotMap[s.startTime][s.weekday] = []
    slotMap[s.startTime][s.weekday].push(s)
  }

  return (
    <div className="print-page">
      <div className="print-header">
        <div>
          <h1 className="print-title">{title}</h1>
          <p className="print-subtitle">{subtitle}</p>
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
            <tr key={ts.startTime}>
              <td className="print-td print-td-time">
                <span className="print-time">{ts.startTime}</span>
                <span className="print-time-end">{ts.endTime}</span>
              </td>
              {[1, 2, 3, 4, 5].map((day) => {
                const daySlots = slotMap[ts.startTime]?.[day]
                const hasConflict = daySlots && daySlots.length > 1
                return (
                  <td key={day} className="print-td">
                    {daySlots && daySlots.map((slot, idx) => (
                      <div key={idx} className={`print-cell${hasConflict ? ' print-cell-conflict' : ''}`}>
                        <span className="print-course">{slot.courseName}</span>
                        {slot.teacherName && (
                          <span className="print-info">{slot.teacherName}</span>
                        )}
                        {'className' in slot && (slot as ScheduleSlotDto).className && (
                          <span className="print-info">{(slot as ScheduleSlotDto).className}</span>
                        )}
                        {slot.roomName && (
                          <span className="print-info print-room">{slot.roomName}</span>
                        )}
                        {slot.aideName && (
                          <span className="print-info">{slot.aideName}</span>
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

      {slots.length === 0 && (
        <p className="print-empty">Ingen lektioner at vise</p>
      )}
    </div>
  )
}

function ClassPrintPage({ classId }: { classId: string }) {
  const { data: klass } = useQuery<ClassDto>({
    queryKey: ['class', classId],
    queryFn: () => api.get(`/classes/${classId}`),
  })

  // Get active schema slots via class schedule endpoint
  const { data: schemas } = useQuery<{ id?: string; name?: string | null; isActive?: boolean }[]>({
    queryKey: ['schemas', classId],
    queryFn: () => api.get(`/classes/${classId}/schemas`),
  })

  const activeSchemaId = schemas?.find((s) => s.isActive)?.id

  const { data: detail, isLoading } = useQuery<{
    slots?: { weekday: number; timeSlotId: string; courseName?: string | null; teacherName?: string | null; roomName?: string | null; aideName?: string | null }[]
  }>({
    queryKey: ['schema', classId, activeSchemaId],
    queryFn: () => api.get(`/classes/${classId}/schemas/${activeSchemaId}`),
    enabled: !!activeSchemaId,
  })

  // We also need time slot times — fetch them
  const { data: timeSlots, isLoading: timeSlotsLoading } = useQuery<{ id?: string; startTime?: string; endTime?: string; sortOrder?: number }[]>({
    queryKey: ['time-slots', classId],
    queryFn: () => api.get(`/classes/${classId}/time-slots`),
    enabled: !!classId,
  })

  const tsMap: Record<string, { startTime: string; endTime: string; sortOrder: number }> = {}
  for (const ts of timeSlots ?? []) {
    if (ts.id && typeof ts.startTime === 'string' && typeof ts.endTime === 'string') {
      tsMap[ts.id] = {
        startTime: ts.startTime.slice(0, 5),
        endTime: ts.endTime.slice(0, 5),
        sortOrder: ts.sortOrder ?? 0,
      }
    }
  }

  const slots: ClassSchemaSlot[] = (detail?.slots ?? []).map((s) => ({
    weekday: s.weekday,
    startTime: tsMap[s.timeSlotId]?.startTime ?? '',
    endTime: tsMap[s.timeSlotId]?.endTime ?? '',
    sortOrder: tsMap[s.timeSlotId]?.sortOrder ?? 0,
    courseName: s.courseName ?? '',
    teacherName: s.teacherName,
    roomName: s.roomName,
    aideName: s.aideName,
  }))

  // Wait for all queries to complete before rendering
  const isLoading = detail?.slots === undefined || timeSlotsLoading

  if (isLoading) return <div className="p-8 text-gray-400">Henter skema…</div>

  return (
    <PrintGrid
      title={klass?.name ?? 'Klasse'}
      subtitle="Ugeskema"
      slots={slots}
    />
  )
}

function StaffPrintPage({ staffId }: { staffId: string }) {
  const { data: staff } = useQuery<StaffDto>({
    queryKey: ['staff', staffId],
    queryFn: () => api.get(`/staff/${staffId}`),
  })

  const { data: slots = [], isLoading } = useQuery<ScheduleSlotDto[]>({
    queryKey: ['staff-schedule', staffId],
    queryFn: () => api.get(`/staff/${staffId}/schedule`),
  })

  if (isLoading) return <div className="p-8 text-gray-400">Henter skema…</div>

  return (
    <PrintGrid
      title={staff?.name ?? 'Medarbejder'}
      subtitle="Ugeskema på tværs af klasser"
      slots={slots}
    />
  )
}

function RoomPrintPage({ roomId }: { roomId: string }) {
  const { data: room } = useQuery<RoomDto>({
    queryKey: ['room', roomId],
    queryFn: () => api.get(`/rooms/${roomId}`),
  })

  const { data: slots = [], isLoading } = useQuery<ScheduleSlotDto[]>({
    queryKey: ['room-schedule', roomId],
    queryFn: () => api.get(`/rooms/${roomId}/schedule`),
  })

  if (isLoading) return <div className="p-8 text-gray-400">Henter lokaleplan…</div>

  return (
    <PrintGrid
      title={room?.name ?? 'Lokale'}
      subtitle="Ugentlig belægning"
      slots={slots}
    />
  )
}

export default function PrintSchemaPage() {
  const { classId, staffId, roomId } = useParams<{ classId?: string; staffId?: string; roomId?: string }>()
  const [searchParams] = useSearchParams()
  
  // Safely validate PrintMode from query param
  const typeParam = searchParams.get('type')
  const isValidPrintMode = (value: string | null): value is PrintMode => {
    return value === 'class' || value === 'staff' || value === 'room'
  }
  
  const mode: PrintMode = isValidPrintMode(typeParam) 
    ? typeParam 
    : (classId ? 'class' : staffId ? 'staff' : 'room')

  return (
    <>
      <style>{`
        @media print {
          body { margin: 0; }
          .no-print { display: none !important; }
        }
        .print-page {
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
          padding: 24px;
          max-width: 210mm;
          margin: 0 auto;
        }
        .print-header {
          display: flex;
          justify-content: space-between;
          align-items: flex-end;
          margin-bottom: 16px;
          padding-bottom: 12px;
          border-bottom: 2px solid #1e3a5f;
        }
        .print-title {
          font-size: 20px;
          font-weight: 700;
          color: #111827;
          margin: 0;
        }
        .print-subtitle {
          font-size: 13px;
          color: #6b7280;
          margin: 2px 0 0;
        }
        .print-date {
          font-size: 11px;
          color: #9ca3af;
          margin: 0;
        }
        .print-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 11px;
        }
        .print-th {
          background: #f3f4f6;
          text-align: center;
          padding: 6px 4px;
          font-weight: 600;
          color: #374151;
          border: 1px solid #e5e7eb;
          font-size: 11px;
          text-transform: uppercase;
          letter-spacing: 0.04em;
        }
        .print-th-time {
          width: 64px;
          text-align: right;
        }
        .print-td {
          border: 1px solid #e5e7eb;
          padding: 4px 6px;
          vertical-align: top;
          min-height: 48px;
          width: calc((100% - 64px) / 5);
        }
        .print-td-time {
          text-align: right;
          padding: 4px 8px 4px 4px;
          background: #f9fafb;
          white-space: nowrap;
          width: 64px;
        }
        .print-time {
          display: block;
          font-weight: 600;
          font-size: 11px;
          color: #374151;
        }
        .print-time-end {
          display: block;
          font-size: 10px;
          color: #9ca3af;
        }
        .print-cell {
          display: flex;
          flex-direction: column;
          gap: 1px;
        }
        .print-course {
          font-weight: 600;
          color: #111827;
          font-size: 11px;
        }
        .print-info {
          font-size: 10px;
          color: #6b7280;
        }
        .print-room {
          color: #9ca3af;
        }
        .print-empty {
          text-align: center;
          color: #9ca3af;
          margin-top: 32px;
          font-size: 13px;
        }
      `}</style>

      <div className="no-print fixed top-4 right-4 flex gap-2 z-50">
        <button
          onClick={() => window.print()}
          className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 shadow transition-colors"
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="6 9 6 2 18 2 18 9" />
            <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
            <rect x="6" y="14" width="12" height="8" />
          </svg>
          Udskriv
        </button>
        <button
          onClick={() => window.close()}
          className="px-4 py-2 bg-white text-gray-700 text-sm font-medium rounded-lg border border-gray-200 hover:bg-gray-50 shadow transition-colors"
        >
          Luk
        </button>
      </div>

      {mode === 'class' && classId && <ClassPrintPage classId={classId} />}
      {mode === 'staff' && staffId && <StaffPrintPage staffId={staffId} />}
      {mode === 'room' && roomId && <RoomPrintPage roomId={roomId} />}
    </>
  )
}
