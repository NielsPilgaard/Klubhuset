import { useState, useMemo, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  DndContext,
  DragOverlay,
  DragOverEvent,
  PointerSensor,
  useSensor,
  useSensors,
  useDroppable,
  useDraggable,
  DragStartEvent,
  DragEndEvent,
  closestCenter,
} from '@dnd-kit/core'
import { CSS } from '@dnd-kit/utilities'
import { usePageTitle } from '../hooks/usePageTitle'
import {
  api,
  SchemaDetailDto,
  SlotsAndConflictsDto,
  TimeSlotDto,
  CourseDto,
  StaffDto,
  RoomDto,
  SlotDto,
  ConflictInfo,
} from '../api/client'

const WEEKDAYS = ['Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']
const WEEKDAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] as const
type WeekdayName = typeof WEEKDAY_NAMES[number]

function weekdayLabel(day: string | undefined): string {
  if (!day) return ''
  const idx = WEEKDAY_NAMES.indexOf(day as WeekdayName)
  return idx >= 0 ? WEEKDAYS[idx] : day
}

const SESSION_KEY_COURSE = 'schema-last-courseId'
const SESSION_KEY_TEACHER = 'schema-last-teacherId'

const COURSE_COLORS = [
  'bg-blue-100 text-blue-800 border-blue-200',
  'bg-purple-100 text-purple-800 border-purple-200',
  'bg-teal-100 text-teal-800 border-teal-200',
  'bg-orange-100 text-orange-800 border-orange-200',
  'bg-pink-100 text-pink-800 border-pink-200',
  'bg-indigo-100 text-indigo-800 border-indigo-200',
  'bg-cyan-100 text-cyan-800 border-cyan-200',
  'bg-yellow-100 text-yellow-800 border-yellow-200',
  'bg-lime-100 text-lime-800 border-lime-200',
  'bg-rose-100 text-rose-800 border-rose-200',
]

function getCourseColor(
  courseId: string,
  courseIds: string[],
  courses?: import('../api/client').CourseDto[],
): { colorClass: string; colorStyle?: React.CSSProperties } {
  const hex = courses?.find((c) => c.id === courseId)?.color
  if (hex) {
    // Derive a readable text color: use a dark tint of the hex for text/border
    return {
      colorClass: 'border',
      colorStyle: { backgroundColor: hex + '22', color: hex, borderColor: hex + '66' },
    }
  }
  const idx = courseIds.indexOf(courseId)
  return { colorClass: COURSE_COLORS[idx % COURSE_COLORS.length] }
}

// Encode/decode drag IDs as "timeSlotId:weekday"
function encodeDragId(timeSlotId: string, weekday: number) {
  return `${timeSlotId}:${weekday}`
}
function decodeDragId(id: string): { timeSlotId: string; weekday: number } {
  const [timeSlotId, weekdayStr] = id.split(':')
  return { timeSlotId, weekday: parseInt(weekdayStr, 10) }
}

// ─── Searchable combobox ─────────────────────────────────────────────────────

interface SearchableSelectProps {
  label: string
  required?: boolean
  options: { id: string; label: string }[]
  value: string
  query: string
  onQueryChange: (q: string) => void
  onChange: (id: string) => void
  placeholder?: string
  emptyLabel?: string
  'data-testid'?: string
}

function SearchableSelect({
  label, required, options, value, query, onQueryChange, onChange,
  placeholder = 'Søg...', emptyLabel = '—', 'data-testid': testId,
}: SearchableSelectProps) {
  const [open, setOpen] = useState(false)

  // When a value is selected, show full list; otherwise filter by query
  const filtered = (value || query.trim() === '')
    ? options
    : options.filter((o) => o.label.toLowerCase().includes(query.toLowerCase()))

  const selectedLabel = value ? (options.find((o) => o.id === value)?.label ?? '') : ''

  function select(opt: { id: string; label: string } | null) {
    if (opt) {
      onChange(opt.id)
      onQueryChange('')
    } else {
      onChange('')
      onQueryChange('')
    }
    setOpen(false)
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    onQueryChange(e.target.value)
    if (value) onChange('') // clear chip when user starts typing
    setOpen(true)
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Escape') { setOpen(false); return }
    if (e.key === 'Backspace' && value && query === '') {
      // Clear chip on backspace when input is empty
      onChange('')
      onQueryChange('')
      setOpen(true)
      return
    }
    if (e.key === 'Enter') {
      if (open && !value && filtered.length > 0) {
        // Dropdown open, no chip yet — select the first match
        e.preventDefault()
        select(filtered[0])
        return
      }
      // Either dropdown closed or chip already selected — close dropdown and submit
      setOpen(false)
      // Do NOT call e.preventDefault() — let the form's onSubmit fire
    }
  }

  return (
    <div className="relative">
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {label}{required && <span className="text-red-500 ml-0.5">*</span>}
      </label>
      <div className={`flex items-center gap-1 flex-wrap w-full px-3 py-1.5 border border-gray-300 rounded-lg bg-white focus-within:ring-2 focus-within:ring-brand-500 focus-within:border-transparent`}>
        {value && (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-brand-100 text-brand-800 text-xs font-medium rounded-md shrink-0">
            {selectedLabel}
            <button
              type="button"
              tabIndex={-1}
              onMouseDown={(e) => { e.preventDefault(); select(null) }}
              className="ml-0.5 text-brand-500 hover:text-brand-800 leading-none"
              aria-label="Fjern valg"
            >×</button>
          </span>
        )}
        <input
          type="text"
          autoComplete="off"
          value={query}
          onChange={handleInputChange}
          onFocus={() => setOpen(true)}
          onBlur={() => setTimeout(() => setOpen(false), 150)}
          onKeyDown={handleKeyDown}
          placeholder={value ? '' : placeholder}
          data-testid={testId}
          className="flex-1 min-w-0 py-0.5 text-sm focus:outline-none bg-transparent"
          style={{ minWidth: '4rem' }}
        />
        {!value && query === '' && (
          <span className="pointer-events-none text-xs text-gray-400 ml-auto">{emptyLabel}</span>
        )}
      </div>
      {open && (
        <ul className="absolute z-10 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg max-h-52 overflow-y-auto text-sm">
          {!required && (
            <li
              onMouseDown={() => select(null)}
              className="px-3 py-2 text-gray-400 hover:bg-gray-50 cursor-pointer"
            >{emptyLabel}</li>
          )}
          {filtered.map((o) => (
            <li
              key={o.id}
              onMouseDown={() => select(o)}
              className={`px-3 py-2 cursor-pointer hover:bg-brand-50 hover:text-brand-700 ${o.id === value ? 'bg-brand-50 text-brand-700 font-medium' : 'text-gray-800'}`}
            >{o.label}</li>
          ))}
          {filtered.length === 0 && (
            <li className="px-3 py-2 text-gray-400 italic">Ingen resultater</li>
          )}
        </ul>
      )}
    </div>
  )
}

// ─── Assignment panel ────────────────────────────────────────────────────────

interface AssignmentPanelProps {
  classId: string
  schemaId: string
  timeSlotId: string
  weekday: number
  existing: SlotDto | null
  courses: CourseDto[]
  staff: StaffDto[]
  rooms: RoomDto[]
  onClose: () => void
  onSaved: (updated: SlotsAndConflictsDto) => void
}

function AssignmentPanel({
  classId, schemaId, timeSlotId, weekday, existing,
  courses, staff, rooms, onClose, onSaved,
}: AssignmentPanelProps) {
  const [courseId, setCourseId] = useState(
    existing?.courseId ?? sessionStorage.getItem(SESSION_KEY_COURSE) ?? ''
  )
  const [teacherId, setTeacherId] = useState(
    existing?.teacherId ?? sessionStorage.getItem(SESSION_KEY_TEACHER) ?? ''
  )
  const [roomId, setRoomId] = useState(existing?.roomId ?? '')
  const [aideId, setAideId] = useState(existing?.aideId ?? '')

  const teachers = staff.filter((s) => s.role === 'Teacher')
  const aides = staff.filter((s) => s.role === 'Aide' || s.role === 'Substitute')

  const saveMutation = useMutation({
    mutationFn: () =>
      api.put<SlotsAndConflictsDto>(`/classes/${classId}/schemas/${schemaId}/slots`, {
        timeSlotId, weekday, courseId, teacherId,
        roomId: roomId || null,
        aideId: aideId || null,
      }),
    onSuccess: (data) => {
      sessionStorage.setItem(SESSION_KEY_COURSE, courseId)
      sessionStorage.setItem(SESSION_KEY_TEACHER, teacherId)
      onSaved(data)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () =>
      api.delete(`/classes/${classId}/schemas/${schemaId}/slots/${timeSlotId}/${weekday}`),
    onSuccess: onClose,
  })

  // ─── Combobox state ─────────────────────────────────────────────────────────
  // With chip design, query starts empty whenever the field already has a value
  const [courseQuery, setCourseQuery] = useState('')
  const [teacherQuery, setTeacherQuery] = useState('')
  const [roomQuery, setRoomQuery] = useState('')
  const [aideQuery, setAideQuery] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!(courseId && teacherId) || saveMutation.isPending) return
    saveMutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40" onClick={onClose}>
      <form
        className="bg-white rounded-2xl shadow-xl w-full max-w-md"
        onClick={(e) => e.stopPropagation()}
        onSubmit={handleSubmit}
      >
        <div className="px-6 py-5 border-b border-gray-100 flex items-center justify-between">
          <h2 className="font-display text-lg font-semibold text-gray-900">{WEEKDAYS[weekday - 1]}</h2>
          <button type="button" onClick={onClose} className="p-1 text-gray-400 hover:text-gray-600 rounded-md">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <button type="submit" className="hidden" aria-hidden="true" tabIndex={-1} />
        <div className="px-6 py-5 space-y-4">
          <SearchableSelect
            label="Fag"
            required
            options={courses.map((c) => ({ id: c.id ?? '', label: c.name ?? '' }))}
            value={courseId}
            query={courseQuery}
            onQueryChange={setCourseQuery}
            onChange={(id) => setCourseId(id)}
            placeholder="Søg efter fag..."
            data-testid="slot-course-input"
          />
          <SearchableSelect
            label="Lærer"
            required
            options={teachers.map((s) => ({ id: s.id ?? '', label: s.name ?? '' }))}
            value={teacherId}
            query={teacherQuery}
            onQueryChange={setTeacherQuery}
            onChange={(id) => setTeacherId(id)}
            placeholder="Søg efter lærer..."
            data-testid="slot-teacher-input"
          />
          <SearchableSelect
            label="Lokale"
            options={rooms.map((r) => ({ id: r.id ?? '', label: r.name ?? '' }))}
            value={roomId}
            query={roomQuery}
            onQueryChange={setRoomQuery}
            onChange={(id) => setRoomId(id)}
            placeholder="Søg efter lokale..."
            emptyLabel="Intet lokale"
            data-testid="slot-room-input"
          />
          <SearchableSelect
            label="Pædagog / Vikar"
            options={aides.map((s) => ({ id: s.id ?? '', label: s.name ?? '' }))}
            value={aideId}
            query={aideQuery}
            onQueryChange={setAideQuery}
            onChange={(id) => setAideId(id)}
            placeholder="Søg efter pædagog..."
            emptyLabel="Ingen"
            data-testid="slot-aide-input"
          />
          {(saveMutation.isError || deleteMutation.isError) && (
            <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
          )}
        </div>

        <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between">
          {existing ? (
            <button type="button" onClick={() => deleteMutation.mutate()} disabled={deleteMutation.isPending}
              className="px-3 py-1.5 text-sm text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50">
              {deleteMutation.isPending ? 'Sletter...' : 'Slet lektion'}
            </button>
          ) : <span />}
          <div className="flex gap-3">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 transition-colors">
              Annuller
            </button>
            <button type="submit"
              disabled={!(courseId && teacherId) || saveMutation.isPending}
              className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
              {saveMutation.isPending ? 'Gemmer...' : 'Gem'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}

// ─── Slot card (used both in grid and DragOverlay) ───────────────────────────

interface SlotCardProps {
  slot: SlotDto
  isConflict: boolean
  colorClass: string
  colorStyle?: React.CSSProperties
  isDragging?: boolean
}

function SlotCard({ slot, isConflict, colorClass, colorStyle, isDragging }: SlotCardProps) {
  if (isConflict) {
    return (
      <div className={`h-full w-full flex flex-col gap-0.5 p-2 rounded-lg border-2 border-red-300 bg-red-50 text-left select-none ${isDragging ? 'opacity-80 shadow-lg rotate-1' : ''}`}>
        <div className="flex items-start justify-between gap-1">
          <span className="text-xs font-semibold text-red-800 leading-tight line-clamp-1">{slot.courseName}</span>
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-red-500 shrink-0 mt-0.5">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
            <line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" />
          </svg>
        </div>
        <div className="flex items-center gap-1">
          <span className="text-xs text-red-600 leading-tight line-clamp-1 flex-1">{slot.teacherName}</span>
          {slot.teacherName && (
            <span className="text-xs text-red-500 font-medium shrink-0">{deriveInitials(slot.teacherName)}</span>
          )}
        </div>
        {slot.roomName && <span className="text-xs text-red-400 leading-tight line-clamp-1">{slot.roomName}</span>}
      </div>
    )
  }

  return (
    <div className={`h-full w-full flex flex-col gap-0.5 p-2 rounded-lg border text-left select-none ${colorClass} ${isDragging ? 'opacity-80 shadow-lg rotate-1' : ''}`} style={colorStyle}>
      <span className="text-xs font-semibold leading-tight line-clamp-2">{slot.courseName}</span>
      <div className="flex items-center gap-1">
        <span className="text-xs opacity-75 leading-tight line-clamp-1 flex-1">{slot.teacherName}</span>
        {slot.teacherName && (
          <span className="text-xs text-gray-500 font-medium shrink-0">{deriveInitials(slot.teacherName)}</span>
        )}
      </div>
      {slot.roomName && <span className="text-xs opacity-60 leading-tight line-clamp-1">{slot.roomName}</span>}
    </div>
  )
}

// ─── Draggable cell wrapper ──────────────────────────────────────────────────

interface DraggableCellProps {
  dragId: string
  slot: SlotDto
  isConflict: boolean
  colorClass: string
  colorStyle?: React.CSSProperties
  onClick: () => void
  isBeingDragged: boolean
}

function DraggableCell({ dragId, slot, isConflict, colorClass, colorStyle, onClick, isBeingDragged }: DraggableCellProps) {
  const { attributes, listeners, setNodeRef, transform } = useDraggable({ id: dragId })

  const style = transform
    ? { transform: CSS.Translate.toString(transform), zIndex: 10, position: 'relative' as const }
    : undefined

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...listeners}
      {...attributes}
      className={`h-full w-full cursor-grab active:cursor-grabbing ${isBeingDragged ? 'opacity-30' : ''}`}
      onClick={(e) => {
        // Only open panel on click, not after a drag
        if (!transform) onClick()
        e.stopPropagation()
      }}
    >
      <SlotCard slot={slot} isConflict={isConflict} colorClass={colorClass} colorStyle={colorStyle} />
    </div>
  )
}

// ─── Droppable cell wrapper ──────────────────────────────────────────────────

interface DroppableCellProps {
  dropId: string
  children: React.ReactNode
  isOver: boolean
  isEmpty: boolean
  onClick?: () => void
}

function DroppableCell({ dropId, children, isOver, isEmpty, onClick }: DroppableCellProps) {
  const { setNodeRef } = useDroppable({ id: dropId })

  return (
    <div
      ref={setNodeRef}
      className={`h-full w-full transition-all rounded-lg ${isOver && isEmpty ? 'ring-2 ring-brand-400 ring-offset-1 bg-brand-50' : ''} ${isOver && !isEmpty ? 'ring-2 ring-brand-400 ring-offset-1' : ''}`}
      onClick={onClick}
    >
      {children}
    </div>
  )
}

// ─── Empty cell ──────────────────────────────────────────────────────────────

function EmptyCell({ onClick }: { onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="group h-full w-full flex items-center justify-center rounded-lg border-2 border-dashed border-gray-200 hover:border-brand-400 hover:bg-brand-50 transition-all"
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
        className="text-gray-300 group-hover:text-brand-400 transition-colors">
        <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
      </svg>
    </button>
  )
}

function deriveInitials(name: string | null | undefined): string {
  if (!name) return ''
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}


// ─── Main page ───────────────────────────────────────────────────────────────

export default function SchemaBuilderPage() {
  usePageTitle('Skema')
  const { classId, schemaId } = useParams<{ classId: string; schemaId: string }>()
  const qc = useQueryClient()

  const [panelCell, setPanelCell] = useState<{ timeSlotId: string; weekday: number } | null>(null)
  const [localSlots, setLocalSlots] = useState<SlotDto[] | null>(null)
  const [localConflicts, setLocalConflicts] = useState<ConflictInfo[] | null>(null)
  const [activeDragId, setActiveDragId] = useState<string | null>(null)
  const [overDropId, setOverDropId] = useState<string | null>(null)

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } })
  )

  const { data: detail, isLoading: loadingDetail, isError: errorDetail } = useQuery<SchemaDetailDto>({
    queryKey: ['schema', classId, schemaId],
    queryFn: () => api.get(`/classes/${classId}/schemas/${schemaId}`),
    enabled: !!classId && !!schemaId,
  })

  const { data: timeSlots, isLoading: loadingTs } = useQuery<TimeSlotDto[]>({
    queryKey: ['time-slots', classId, schemaId],
    queryFn: () => api.get(`/classes/${classId}/schemas/${schemaId}/time-slots`),
    enabled: !!classId && !!schemaId,
  })

  const { data: courses } = useQuery<CourseDto[]>({
    queryKey: ['courses'],
    queryFn: () => api.get('/courses'),
  })

  const { data: staff } = useQuery<StaffDto[]>({
    queryKey: ['staff'],
    queryFn: () => api.get('/staff'),
  })

  const { data: rooms } = useQuery<RoomDto[]>({
    queryKey: ['rooms'],
    queryFn: () => api.get('/rooms'),
  })

  const completeMutation = useMutation({
    mutationFn: () => api.post(`/classes/${classId}/schemas/${schemaId}/complete`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['schema', classId, schemaId] }),
  })

  const activateMutation = useMutation({
    mutationFn: () => api.post(`/classes/${classId}/schemas/${schemaId}/activate`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['schema', classId, schemaId] }),
  })

  const upsertSlotMutation = useMutation({
    mutationFn: (payload: { timeSlotId: string; weekday: number; courseId: string; teacherId: string; roomId: string | null; aideId: string | null }) =>
      api.put<SlotsAndConflictsDto>(`/classes/${classId}/schemas/${schemaId}/slots`, payload),
    onSuccess: (data) => {
      setLocalSlots(data.slots ?? null)
      setLocalConflicts(data.conflicts ?? null)
      qc.setQueryData<SchemaDetailDto>(['schema', classId, schemaId], (old) =>
        old ? { ...old, slots: data.slots, conflicts: data.conflicts } : old
      )
    },
  })

  const slots = useMemo(() => localSlots ?? detail?.slots ?? [], [localSlots, detail?.slots])
  const conflicts = useMemo(() => localConflicts ?? detail?.conflicts ?? [], [localConflicts, detail?.conflicts])
  const schema = detail?.schema

  const slotMap = useMemo(() => {
    const map: Record<string, Record<number, SlotDto>> = {}
    for (const s of slots) {
      if (!s.timeSlotId || s.weekday === undefined) continue
      const weekdayNum = typeof s.weekday === 'number'
        ? s.weekday
        : WEEKDAY_NAMES.indexOf(s.weekday as WeekdayName) + 1
      if (!map[s.timeSlotId]) map[s.timeSlotId] = {}
      map[s.timeSlotId][weekdayNum] = s
    }
    return map
  }, [slots])

  const conflictSlotIds = useMemo(() => {
    const ids = new Set<string>()
    for (const c of conflicts) {
      if (c.slotAId) ids.add(c.slotAId)
      if (c.slotBId) ids.add(c.slotBId)
    }
    return ids
  }, [conflicts])

  const courseIds = useMemo(() => {
    const seen: string[] = []
    for (const s of slots) {
      if (s.courseId && !seen.includes(s.courseId)) seen.push(s.courseId)
    }
    return seen
  }, [slots])

  const sortedTimeSlots = useMemo(
    () => [...(timeSlots ?? [])].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
    [timeSlots]
  )

  const handleCellSaved = useCallback((updated: SlotsAndConflictsDto) => {
    setLocalSlots(updated.slots ?? null)
    setLocalConflicts(updated.conflicts ?? null)
    setPanelCell(null)
    qc.setQueryData<SchemaDetailDto>(['schema', classId, schemaId], (old) =>
      old ? { ...old, slots: updated.slots, conflicts: updated.conflicts } : old
    )
  }, [qc, classId, schemaId])

  const handleCellDeleted = useCallback(() => {
    setPanelCell(null)
    qc.invalidateQueries({ queryKey: ['schema', classId, schemaId] })
    setLocalSlots(null)
    setLocalConflicts(null)
  }, [qc, classId, schemaId])

  // ─── Drag handlers ────────────────────────────────────────────────────────

  const handleDragStart = useCallback((event: DragStartEvent) => {
    setActiveDragId(event.active.id as string)
  }, [])

  const handleDragOver = useCallback((event: DragOverEvent) => {
    setOverDropId(event.over ? String(event.over.id) : null)
  }, [])

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    setActiveDragId(null)
    setOverDropId(null)

    const { active, over } = event
    if (!over || active.id === over.id) return

    const src = decodeDragId(active.id as string)
    const dst = decodeDragId(over.id as string)

    const srcSlot = slotMap[src.timeSlotId]?.[src.weekday]
    const dstSlot = slotMap[dst.timeSlotId]?.[dst.weekday]

    // Don't allow drops onto break rows
    const dstTs = sortedTimeSlots.find((ts) => ts.id === dst.timeSlotId)
    if (dstTs?.isBreak) return

    if (!srcSlot) return

    if (dstSlot) {
      // Swap: put dst's assignment into src's cell, src's into dst's cell
      upsertSlotMutation.mutate({
        timeSlotId: dst.timeSlotId,
        weekday: dst.weekday,
        courseId: srcSlot.courseId ?? '',
        teacherId: srcSlot.teacherId ?? '',
        roomId: srcSlot.roomId ?? null,
        aideId: srcSlot.aideId ?? null,
      })
      upsertSlotMutation.mutate({
        timeSlotId: src.timeSlotId,
        weekday: src.weekday,
        courseId: dstSlot.courseId ?? '',
        teacherId: dstSlot.teacherId ?? '',
        roomId: dstSlot.roomId ?? null,
        aideId: dstSlot.aideId ?? null,
      })
    } else {
      // Move: optimistically remove from source immediately so no ghost appears
      setLocalSlots((prev) => {
        const base = prev ?? detail?.slots ?? []
        return base.filter(
          (s) => !(s.timeSlotId === src.timeSlotId && (
            (typeof s.weekday === 'number' ? s.weekday : WEEKDAY_NAMES.indexOf(s.weekday as WeekdayName) + 1) === src.weekday
          ))
        )
      })
      upsertSlotMutation.mutate({
        timeSlotId: dst.timeSlotId,
        weekday: dst.weekday,
        courseId: srcSlot.courseId ?? '',
        teacherId: srcSlot.teacherId ?? '',
        roomId: srcSlot.roomId ?? null,
        aideId: srcSlot.aideId ?? null,
      })
      api.delete(`/classes/${classId}/schemas/${schemaId}/slots/${src.timeSlotId}/${src.weekday}`)
        .then(() => qc.invalidateQueries({ queryKey: ['schema', classId, schemaId] }))
    }
  }, [slotMap, sortedTimeSlots, upsertSlotMutation, classId, schemaId, qc])

  // Active drag slot (for overlay)
  const activeDragSlot = useMemo(() => {
    if (!activeDragId) return null
    const { timeSlotId, weekday } = decodeDragId(activeDragId)
    return slotMap[timeSlotId]?.[weekday] ?? null
  }, [activeDragId, slotMap])

  const activeDragColor = activeDragSlot
    ? getCourseColor(activeDragSlot.courseId ?? '', courseIds, courses)
    : { colorClass: '' }
  const activeDragConflict = activeDragSlot
    ? conflictSlotIds.has(activeDragSlot.id ?? '')
    : false

  const isLoading = loadingDetail || loadingTs
  const hasConflicts = conflicts.length > 0

  const openPanel = panelCell
    ? { ...panelCell, existing: slotMap[panelCell.timeSlotId]?.[panelCell.weekday] ?? null }
    : null

  if (errorDetail) {
    return (
      <div className="p-8">
        <div className="bg-red-50 border border-red-200 rounded-xl p-6 text-center">
          <p className="text-red-700 font-medium">Kunne ikke hente skema</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Status bar */}
      <div className="shrink-0 bg-white border-b border-gray-200 px-4 lg:px-6 py-3 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-3 min-w-0">
          <Link to="/klasser" className="text-gray-400 hover:text-gray-600 transition-colors shrink-0">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </Link>
          {isLoading ? (
            <div className="h-5 w-36 bg-gray-200 rounded animate-pulse" />
          ) : (
            <>
              <h1 className="font-display text-base font-semibold text-gray-900 truncate">{schema?.name}</h1>
              <span className={`shrink-0 px-2 py-0.5 text-xs font-medium rounded-full ${schema?.status === 'Complete' ? 'bg-brand-100 text-brand-700' : 'bg-amber-100 text-amber-700'}`}>
                {schema?.status === 'Complete' ? 'Færdig' : 'Kladde'}
              </span>
              {schema?.isActive && (
                <span className="shrink-0 px-2 py-0.5 text-xs font-medium rounded-full bg-brand-600 text-white">Aktiv</span>
              )}
            </>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Link to={`/klasser/${classId}/schemas/${schemaId}/lektioner`}
            className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
            title="Tilpas lektionsstruktur">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
              <line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" />
            </svg>
          </Link>
          <a href={`/udskriv/klasse/${classId}`} target="_blank" rel="noopener noreferrer"
            className="p-1.5 text-gray-400 hover:text-gray-700 rounded-md hover:bg-gray-100 transition-colors"
            title="Udskriv skema">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="6 9 6 2 18 2 18 9" />
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
              <rect x="6" y="14" width="12" height="8" />
            </svg>
          </a>
          {hasConflicts && (
            <span className="flex items-center gap-1.5 px-3 py-1.5 bg-red-50 text-red-600 text-xs font-medium rounded-lg border border-red-200">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                <line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" />
              </svg>
              {conflicts.length} konflikt{conflicts.length !== 1 ? 'er' : ''}
            </span>
          )}
          {schema?.status !== 'Complete' && (
            <button onClick={() => completeMutation.mutate()}
              disabled={hasConflicts || completeMutation.isPending}
              title={hasConflicts ? 'Løs konflikter først' : undefined}
              className="px-3 py-1.5 text-xs font-medium bg-brand-50 text-brand-700 border border-brand-200 rounded-lg hover:bg-brand-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors">
              Markér som færdig
            </button>
          )}
          {!schema?.isActive && (
            <button onClick={() => activateMutation.mutate()} disabled={activateMutation.isPending}
              className="px-3 py-1.5 text-xs font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors">
              Aktivér
            </button>
          )}
        </div>
      </div>

      {/* Main grid */}
      <div className="flex-1 overflow-auto">
        <div className="p-4 lg:p-6">
          {isLoading ? (
            <div className="animate-pulse">
              <div className="grid grid-cols-6 gap-2 mb-2">
                {Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-8 bg-gray-200 rounded" />)}
              </div>
              {Array.from({ length: 6 }).map((_, r) => (
                <div key={r} className="grid grid-cols-6 gap-2 mb-2">
                  {Array.from({ length: 6 }).map((_, c) => <div key={c} className="h-20 bg-gray-100 rounded-lg" />)}
                </div>
              ))}
            </div>
          ) : (
            <DndContext
              sensors={sensors}
              collisionDetection={closestCenter}
              onDragStart={handleDragStart}
              onDragOver={handleDragOver}
              onDragEnd={handleDragEnd}
            >
              <div className="overflow-x-auto">
                <div className="min-w-[640px]">
                  {/* Header */}
                  <div className="grid grid-cols-[100px_repeat(5,1fr)] gap-2 mb-2">
                    <div />
                    {WEEKDAYS.map((day) => (
                      <div key={day} className="text-center text-xs font-semibold text-gray-500 uppercase tracking-wider py-1">
                        {day}
                      </div>
                    ))}
                  </div>

                  {/* Rows */}
                  <div className="space-y-1.5">
                    {sortedTimeSlots.map((ts) => {
                      if (ts.isBreak) {
                        return (
                          <div key={ts.id} className="grid grid-cols-[100px_repeat(5,1fr)] gap-2">
                            <div className="flex flex-col justify-center text-right pr-3">
                              <span className="text-xs text-gray-400 tabular-nums">{ts.startTime?.slice(0, 5)}</span>
                            </div>
                            <div className="col-span-5 flex items-center gap-2 px-3 py-1.5 bg-gray-50 border border-dashed border-gray-200 rounded-lg">
                              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-gray-400 shrink-0">
                                <path d="M18 8h1a4 4 0 0 1 0 8h-1" />
                                <path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4V8z" />
                                <line x1="6" y1="1" x2="6" y2="4" /><line x1="10" y1="1" x2="10" y2="4" /><line x1="14" y1="1" x2="14" y2="4" />
                              </svg>
                              <span className="text-xs text-gray-400">
                                Pause · {ts.startTime?.slice(0, 5)}–{ts.endTime?.slice(0, 5)}
                              </span>
                            </div>
                          </div>
                        )
                      }

                      return (
                        <div key={ts.id} className="grid grid-cols-[100px_repeat(5,1fr)] gap-2">
                          {/* Time label */}
                          <div className="flex flex-col justify-center text-right pr-3">
                            <span className="text-xs font-medium text-gray-600 tabular-nums">{ts.startTime?.slice(0, 5)}</span>
                            <span className="text-xs text-gray-400 tabular-nums">{ts.endTime?.slice(0, 5)}</span>
                            {ts.label && <span className="text-xs text-gray-400 truncate">{ts.label}</span>}
                          </div>

                          {/* Day cells */}
                          {[1, 2, 3, 4, 5].map((weekday) => {
                            const slot = ts.id ? slotMap[ts.id]?.[weekday] ?? null : null
                            const dropId = ts.id ? encodeDragId(ts.id, weekday) : ''
                            const dragId = dropId
                            const isConflict = slot ? conflictSlotIds.has(slot.id ?? '') : false
                            const { colorClass, colorStyle } = slot ? getCourseColor(slot.courseId ?? '', courseIds, courses) : { colorClass: '' }
                            const isCurrentlyDragged = activeDragId === dragId
                            const isOver = overDropId === dropId

                            return (
                              <div key={weekday} className="h-20">
                                <DroppableCell
                                  dropId={dropId}
                                  isOver={isOver}
                                  isEmpty={!slot}
                                  onClick={!slot ? () => ts.id && setPanelCell({ timeSlotId: ts.id, weekday }) : undefined}
                                >
                                  {slot ? (
                                    <DraggableCell
                                      dragId={dragId}
                                      slot={slot}
                                      isConflict={isConflict}
                                      colorClass={colorClass}
                                      colorStyle={colorStyle}
                                      onClick={() => ts.id && setPanelCell({ timeSlotId: ts.id, weekday })}
                                      isBeingDragged={isCurrentlyDragged}
                                    />
                                  ) : (
                                    <EmptyCell onClick={() => ts.id && setPanelCell({ timeSlotId: ts.id, weekday })} />
                                  )}
                                </DroppableCell>
                              </div>
                            )
                          })}
                        </div>
                      )
                    })}
                  </div>

                  {sortedTimeSlots.length === 0 && (
                    <div className="text-center py-16">
                      <p className="text-gray-500 text-sm font-medium">Ingen lektionsstruktur defineret</p>
                      <p className="text-gray-400 text-sm mt-1">
                        Opsæt skoledagens lektioner og pauser, så kan du begynde at bygge skemaet.
                      </p>
                      <Link to={`/klasser/${classId}/schemas/${schemaId}/lektioner`}
                        className="inline-block mt-4 px-4 py-2 text-sm font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors">
                        Opsæt lektionsstruktur
                      </Link>
                    </div>
                  )}
                </div>
              </div>

              {/* Drag overlay — floating card that follows the cursor */}
              <DragOverlay dropAnimation={{ duration: 180, easing: 'cubic-bezier(0.18, 0.67, 0.6, 1.22)' }}>
                {activeDragSlot && (
                  <div className="h-20 w-36 pointer-events-none">
                    <SlotCard
                      slot={activeDragSlot}
                      isConflict={activeDragConflict}
                      {...activeDragColor}
                      isDragging
                    />
                  </div>
                )}
              </DragOverlay>
            </DndContext>
          )}

          {/* Conflicts panel */}
          {!isLoading && hasConflicts && (
            <div className="mt-6 bg-red-50 border border-red-200 rounded-xl overflow-hidden">
              <div className="px-5 py-3 border-b border-red-200 flex items-center gap-2">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-red-500">
                  <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                  <line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" />
                </svg>
                <h3 className="text-sm font-semibold text-red-800">
                  {conflicts.length} konflikt{conflicts.length !== 1 ? 'er' : ''} fundet
                </h3>
              </div>
              <div className="divide-y divide-red-100">
                {conflicts.map((c, i) => {
                  const day = weekdayLabel(c.weekday)
                  const time = `${c.startTime?.slice(0, 5)}–${c.endTime?.slice(0, 5)}`
                  const aLabel = [c.slotACourseName, c.slotAClassName].filter(Boolean).join(' · ')
                  const bLabel = [c.slotBCourseName, c.slotBClassName].filter(Boolean).join(' · ')
                  return (
                    <div key={i} className="px-5 py-3">
                      <p className="text-sm font-medium text-red-800">
                        {c.type === 'TeacherDoubleBooked' && <>Lærer <span className="font-semibold">{c.resourceName}</span> er booket to gange</>}
                        {c.type === 'RoomDoubleBooked' && <>Lokale <span className="font-semibold">{c.resourceName}</span> er booket to gange</>}
                        {c.type === 'AideDoubleBooked' && <>Pædagog <span className="font-semibold">{c.resourceName}</span> er booket to gange</>}
                      </p>
                      <p className="text-xs text-red-600 mt-0.5">
                        {day} {time} — {aLabel} og {bLabel}
                      </p>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Assignment panel */}
      {openPanel && courses && staff && rooms && (
        <AssignmentPanel
          classId={classId!}
          schemaId={schemaId!}
          timeSlotId={openPanel.timeSlotId}
          weekday={openPanel.weekday}
          existing={openPanel.existing}
          courses={courses}
          staff={staff}
          rooms={rooms}
          onClose={handleCellDeleted}
          onSaved={handleCellSaved}
        />
      )}
    </div>
  )
}
