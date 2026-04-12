import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, ClassDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'

// ─── Local types ─────────────────────────────────────────────────────────────

interface WeekPlanSlotFileDto {
  id: string
  schoolFileId: string
  fileName: string
  url: string
}

interface WeekPlanSlotDto {
  id: string
  schemaSlotId: string
  weekday: number
  timeSlotId: string
  timeSlotLabel: string
  startTime: string
  endTime: string
  courseId: string
  courseName: string
  originalCourseId: string | null
  originalCourseName: string | null
  beskrivelse: string | null
  lektier: string | null
  files: WeekPlanSlotFileDto[]
}

interface HolidayDayDto {
  weekday: number
  title: string
}

interface BreakTimeSlotDto {
  timeSlotId: string
  timeSlotLabel: string
  startTime: string
  endTime: string
}

interface WeekPlanDto {
  id: string
  classId: string
  isoYear: number
  isoWeek: number
  weekStartDate: string
  weekEndDate: string
  isHolidayWeek: boolean
  holidayTitle: string | null
  holidayDays: HolidayDayDto[]
  breakSlots: BreakTimeSlotDto[]
  slots: WeekPlanSlotDto[]
}

interface CourseDto {
  id: string
  name: string
}

interface FileDto {
  id: string
  fileName: string
  sizeBytes: number
  url: string
}

interface UpsertWeekPlanSlotRequest {
  schemaSlotId: string
  beskrivelse: string | null
  lektier: string | null
  fagSwapCourseId: string | null
}

// ─── ISO week helpers ─────────────────────────────────────────────────────────

function getISOWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7)
}

function getISOWeekYear(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  return d.getUTCFullYear()
}

function getISOWeeksInYear(year: number): number {
  // A year has 53 weeks if Jan 1 or Dec 31 is Thursday
  const jan1 = new Date(Date.UTC(year, 0, 1)).getUTCDay()
  const dec31 = new Date(Date.UTC(year, 11, 31)).getUTCDay()
  return jan1 === 4 || dec31 === 4 ? 53 : 52
}

// Returns the Monday of the ISO week
function isoWeekToMonday(isoYear: number, isoWeek: number): Date {
  const jan4 = new Date(Date.UTC(isoYear, 0, 4))
  const dayOfWeek = jan4.getUTCDay() || 7
  const weekOneMonday = new Date(jan4.getTime() - (dayOfWeek - 1) * 86400000)
  return new Date(weekOneMonday.getTime() + (isoWeek - 1) * 7 * 86400000)
}

interface SchoolYearWeek {
  isoYear: number
  isoWeek: number
  label: string
}

// School year: Aug 1 – Jun 30. Given current isoYear/isoWeek, generate all weeks.
function getSchoolYearWeeks(isoYear: number, isoWeek: number): SchoolYearWeek[] {
  // Determine which school year: if week is in Aug–Dec, school year starts that calendar year
  const monday = isoWeekToMonday(isoYear, isoWeek)
  const calYear = monday.getUTCFullYear()
  const calMonth = monday.getUTCMonth() // 0-based
  // School year start = Aug 1 of startYear
  const startYear = calMonth >= 7 ? calYear : calYear - 1
  const startDate = new Date(Date.UTC(startYear, 7, 1)) // Aug 1
  const endDate = new Date(Date.UTC(startYear + 1, 5, 30)) // Jun 30

  // Find ISO week of Aug 1 and Jun 30
  const startWeek = getISOWeek(startDate)
  const startWeekYear = getISOWeekYear(startDate)
  const endWeek = getISOWeek(endDate)
  const endWeekYear = getISOWeekYear(endDate)

  const weeks: SchoolYearWeek[] = []
  let y = startWeekYear
  let w = startWeek
  while (y < endWeekYear || (y === endWeekYear && w <= endWeek)) {
    const label = y !== startWeekYear || w === 1
      ? `Uge ${w}, ${y}`
      : `Uge ${w}`
    weeks.push({ isoYear: y, isoWeek: w, label })
    w++
    if (w > getISOWeeksInYear(y)) {
      y++
      w = 1
    }
  }
  return weeks
}

// ─── Course colors (matches SchemaBuilderPage) ────────────────────────────────

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

function getCourseColor(courseId: string, courseIds: string[]): string {
  const idx = courseIds.indexOf(courseId)
  return idx >= 0 ? COURSE_COLORS[idx % COURSE_COLORS.length] : COURSE_COLORS[0]
}

// ─── Constants ────────────────────────────────────────────────────────────────

const WEEKDAYS = ['Mandag', 'Tirsdag', 'Onsdag', 'Torsdag', 'Fredag']
// Monday=1 … Friday=5 (DayOfWeek enum from C#)
const WEEKDAY_NUMS = [1, 2, 3, 4, 5]

// ─── Edit modal ───────────────────────────────────────────────────────────────

interface EditSlotModalProps {
  slot: WeekPlanSlotDto
  classId: string
  isoYear: number
  isoWeek: number
  weekdayLabel: string
  courses: CourseDto[]
  files: FileDto[]
  onClose: () => void
}

function EditSlotModal({ slot, classId, isoYear, isoWeek, weekdayLabel, courses, files, onClose }: EditSlotModalProps) {
  const qc = useQueryClient()
  const [beskrivelse, setBeskrivelse] = useState(slot.beskrivelse ?? '')
  const [lektier, setLektier] = useState(slot.lektier ?? '')
  const [fagSwapCourseId, setFagSwapCourseId] = useState(slot.originalCourseId ? slot.courseId : '')

  const upsertMutation = useMutation({
    mutationFn: (req: UpsertWeekPlanSlotRequest) =>
      api.put<WeekPlanSlotDto>(
        `/classes/${classId}/ugeplan/slots?isoYear=${isoYear}&isoWeek=${isoWeek}`,
        req
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] })
      onClose()
    },
  })

  const addFileMutation = useMutation({
    mutationFn: ({ slotId, schoolFileId }: { slotId: string; schoolFileId: string }) =>
      api.post(`/classes/${classId}/ugeplan/slots/${slotId}/files`, { schoolFileId }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] }),
  })

  const removeFileMutation = useMutation({
    mutationFn: ({ slotId, fileId }: { slotId: string; fileId: string }) =>
      api.delete(`/classes/${classId}/ugeplan/slots/${slotId}/files/${fileId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['weekplan', classId, isoYear, isoWeek] }),
  })

  function handleSave() {
    if (upsertMutation.isPending) return
    upsertMutation.mutate({
      schemaSlotId: slot.schemaSlotId,
      beskrivelse: beskrivelse || null,
      lektier: lektier || null,
      fagSwapCourseId: fagSwapCourseId || null,
    })
  }

  function handleFileToggle(fileId: string, checked: boolean) {
    if (slot.id === '00000000-0000-0000-0000-000000000000') return // no slot id yet, save first
    const existingLink = slot.files.find(f => f.schoolFileId === fileId)
    if (checked && !existingLink) {
      addFileMutation.mutate({ slotId: slot.id, schoolFileId: fileId })
    } else if (!checked && existingLink) {
      removeFileMutation.mutate({ slotId: slot.id, fileId: existingLink.id })
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="px-6 py-5 border-b border-gray-100">
          <h2 className="font-display text-lg font-semibold text-gray-900">
            Rediger lektion — {slot.courseName}, {weekdayLabel}
          </h2>
        </div>

        <div className="px-6 py-5 space-y-5">
          {/* Beskrivelse */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Beskrivelse</label>
            <textarea
              rows={4}
              value={beskrivelse}
              onChange={(e) => setBeskrivelse(e.target.value)}
              placeholder="Hvad skal der ske i denne lektion?"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent resize-none"
            />
          </div>

          {/* Lektier */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Lektier</label>
            <textarea
              rows={3}
              value={lektier}
              onChange={(e) => setLektier(e.target.value)}
              placeholder="Opgaver til næste gang..."
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent resize-none"
            />
          </div>

          {/* Fagbytte */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Fagbytte</label>
            <select
              value={fagSwapCourseId}
              onChange={(e) => setFagSwapCourseId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white"
            >
              <option value="">Intet fagbytte (brug skemaets fag)</option>
              {courses.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          {/* Filer */}
          {files.length > 0 && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Filer</label>
              <div className="space-y-1 max-h-40 overflow-y-auto border border-gray-200 rounded-lg p-2">
                {files.map((f) => {
                  const isChecked = slot.files.some(sf => sf.schoolFileId === f.id)
                  return (
                    <label key={f.id} className="flex items-center gap-2 px-2 py-1.5 rounded hover:bg-gray-50 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={(e) => handleFileToggle(f.id, e.target.checked)}
                        className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                      />
                      <span className="text-sm text-gray-800 truncate flex-1">{f.fileName}</span>
                      <span className="text-xs text-gray-500 shrink-0">
                        {(f.sizeBytes / 1024).toFixed(0)} KB
                      </span>
                    </label>
                  )
                })}
              </div>
            </div>
          )}

          {upsertMutation.isError && (
            <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
          )}
        </div>

        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
          >
            Annuller
          </button>
          <button
            onClick={handleSave}
            disabled={upsertMutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg font-medium hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {upsertMutation.isPending ? 'Gemmer...' : 'Gem'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function WeekPlanPage() {
  usePageTitle('Ugeplan')
  const { classId } = useParams<{ classId: string }>()

  const [isoYear, setIsoYear] = useState(() => getISOWeekYear(new Date()))
  const [isoWeek, setIsoWeek] = useState(() => getISOWeek(new Date()))
  const [editingSlot, setEditingSlot] = useState<WeekPlanSlotDto | null>(null)

  function prevWeek() {
    if (isoWeek === 1) {
      const prevYear = isoYear - 1
      setIsoYear(prevYear)
      setIsoWeek(getISOWeeksInYear(prevYear))
    } else {
      setIsoWeek(isoWeek - 1)
    }
  }

  function nextWeek() {
    const weeksInYear = getISOWeeksInYear(isoYear)
    if (isoWeek === weeksInYear) {
      setIsoYear(isoYear + 1)
      setIsoWeek(1)
    } else {
      setIsoWeek(isoWeek + 1)
    }
  }

  function goToThisWeek() {
    const now = new Date()
    setIsoYear(getISOWeekYear(now))
    setIsoWeek(getISOWeek(now))
  }

  const { data: weekPlanData, isLoading } = useQuery<WeekPlanDto>({
    queryKey: ['weekplan', classId, isoYear, isoWeek],
    queryFn: () => api.get(`/classes/${classId}/ugeplan?isoYear=${isoYear}&isoWeek=${isoWeek}`),
    enabled: !!classId,
  })

  const { data: courses } = useQuery<CourseDto[]>({
    queryKey: ['courses'],
    queryFn: () => api.get('/courses'),
  })

  const { data: files } = useQuery<FileDto[]>({
    queryKey: ['files'],
    queryFn: () => api.get('/files'),
  })

  // Collect all unique course IDs for color assignment
  const allCourseIds = weekPlanData?.slots.map(s => s.courseId).filter((v, i, a) => a.indexOf(v) === i) ?? []

  // Build a combined sorted list of rows: regular lesson slots + break slots
  type GridRow =
    | { kind: 'slot'; timeSlotId: string; timeSlotLabel: string; startTime: string; endTime: string }
    | { kind: 'break'; timeSlotId: string; timeSlotLabel: string; startTime: string; endTime: string }

  const uniqueTimeSlots: GridRow[] = (() => {
    if (!weekPlanData) return []
    const lessonRows: GridRow[] = weekPlanData.slots
      .filter((s, i, a) => a.findIndex(x => x.timeSlotId === s.timeSlotId) === i)
      .map(s => ({ kind: 'slot' as const, timeSlotId: s.timeSlotId, timeSlotLabel: s.timeSlotLabel, startTime: s.startTime, endTime: s.endTime }))
    const breakRows: GridRow[] = (weekPlanData.breakSlots ?? []).map(b => ({
      kind: 'break' as const, timeSlotId: b.timeSlotId, timeSlotLabel: b.timeSlotLabel, startTime: b.startTime, endTime: b.endTime
    }))
    return [...lessonRows, ...breakRows].sort((a, b) => a.startTime.localeCompare(b.startTime))
  })()

  // Parse weekStartDate to compute column dates
  const weekStartDate = weekPlanData?.weekStartDate
    ? new Date(weekPlanData.weekStartDate + 'T00:00:00')
    : null

  const { data: classData } = useQuery<ClassDto[], Error, ClassDto | undefined>({
    queryKey: ['classes'],
    queryFn: () => api.get('/classes'),
    select: (all) => all.find((c) => c.id === classId),
    enabled: !!classId,
  })

  const className = classData?.name ?? ''

  return (
    <div className="flex flex-col h-full">
      {/* Top bar */}
      <div className="shrink-0 bg-white border-b border-gray-200 px-4 lg:px-6 py-3 flex items-center justify-between">
        <div className="flex items-center gap-2 min-w-0">
          <Link
            to="/klasser"
            className="text-gray-400 hover:text-gray-700 transition-colors shrink-0"
            aria-label="Tilbage til klasser"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </Link>
          <h1 className="font-display text-base font-semibold text-gray-900 truncate">
            {className} · Ugeplan
          </h1>
        </div>

        {/* Week navigator */}
        <div className="flex items-center gap-1">
          <button
            onClick={prevWeek}
            className="text-sm text-gray-600 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100 transition-colors"
          >
            ←
          </button>
          <select
            value={`${isoYear}-${isoWeek}`}
            onChange={(e) => {
              const [y, w] = e.target.value.split('-').map(Number)
              setIsoYear(y)
              setIsoWeek(w)
            }}
            className="text-sm font-semibold text-gray-900 px-2 py-1 border border-gray-200 rounded bg-white focus:outline-none focus:ring-2 focus:ring-brand-500"
          >
            {getSchoolYearWeeks(isoYear, isoWeek).map(({ isoYear: y, isoWeek: w, label }) => (
              <option key={`${y}-${w}`} value={`${y}-${w}`}>{label}</option>
            ))}
          </select>
          <button
            onClick={nextWeek}
            className="text-sm text-gray-600 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100 transition-colors"
          >
            →
          </button>
          <button
            onClick={goToThisWeek}
            className="text-xs text-brand-600 hover:underline ml-2"
          >
            Denne uge
          </button>
        </div>

        <div className="w-32" /> {/* spacer for right side */}
      </div>

      {/* Holiday banner */}
      {weekPlanData?.isHolidayWeek && (
        <div className="shrink-0 bg-blue-50 border-b border-blue-200 px-4 lg:px-6 py-2">
          <span className="text-blue-700 text-sm font-medium">
            Feriuge — {weekPlanData.holidayTitle}
          </span>
        </div>
      )}

      {/* Grid area */}
      <div className="flex-1 overflow-y-auto">
        {isLoading && (
          <div className="p-8 text-center text-gray-400 text-sm animate-pulse">Henter ugeplan...</div>
        )}

        {!isLoading && weekPlanData && (
          <div
            className={`grid grid-cols-[auto_1fr_1fr_1fr_1fr_1fr] min-w-0 ${weekPlanData.isHolidayWeek ? 'pointer-events-none opacity-60' : ''}`}
          >
            {/* Header row */}
            <div className="bg-gray-50 border-b border-r border-gray-200 p-2" /> {/* empty corner */}
            {WEEKDAYS.map((label, i) => {
              const weekday = WEEKDAY_NUMS[i]
              const date = weekStartDate ? new Date(weekStartDate.getTime() + i * 86400000) : null
              const dateLabel = date
                ? date.toLocaleDateString('da-DK', { day: '2-digit', month: 'short' })
                : ''
              const holidayDay = (weekPlanData.holidayDays ?? []).find(h => h.weekday === weekday)
              return (
                <div
                  key={label}
                  className={`border-b border-r border-gray-200 p-2 text-center ${holidayDay ? 'bg-amber-50' : 'bg-gray-50'}`}
                >
                  <div className="text-xs font-semibold text-gray-700">{label}</div>
                  {dateLabel && <div className="text-xs text-gray-400">{dateLabel}</div>}
                  {holidayDay && (
                    <div className="text-xs text-amber-600 font-medium mt-0.5 leading-tight">{holidayDay.title}</div>
                  )}
                </div>
              )
            })}

            {/* Data rows */}
            {uniqueTimeSlots.map((ts) => (
              <>
                {/* Time label */}
                <div
                  key={`label-${ts.timeSlotId}`}
                  className={`border-b border-r border-gray-200 p-2 flex flex-col justify-center ${ts.kind === 'break' ? 'bg-gray-100' : 'bg-gray-50'}`}
                >
                  <span className="text-xs text-gray-500 font-mono whitespace-nowrap">{ts.timeSlotLabel}</span>
                  <span className="text-xs text-gray-400 font-mono">{ts.startTime.slice(0, 5)}</span>
                </div>

                {/* Break row: grey separator spanning all columns */}
                {ts.kind === 'break' && WEEKDAY_NUMS.map((_, dayIdx) => (
                  <div
                    key={`break-${ts.timeSlotId}-${dayIdx}`}
                    className="border-b border-r border-gray-200 bg-gray-100 min-h-[28px]"
                  />
                ))}

                {/* Day cells (lesson rows only) */}
                {ts.kind === 'slot' && WEEKDAY_NUMS.map((dayNum, dayIdx) => {
                  const slot = weekPlanData.slots.find(
                    s => s.timeSlotId === ts.timeSlotId && s.weekday === dayNum
                  )
                  const isHolidayCol = (weekPlanData.holidayDays ?? []).some(h => h.weekday === dayNum)

                  if (!slot) {
                    return (
                      <div
                        key={`empty-${ts.timeSlotId}-${dayIdx}`}
                        className={`border-b border-r border-gray-200 min-h-[80px] ${isHolidayCol ? 'bg-amber-50' : 'bg-gray-50'}`}
                      />
                    )
                  }

                  const colorClass = getCourseColor(slot.courseId, allCourseIds)

                  return (
                    <div
                      key={`slot-${slot.schemaSlotId}`}
                      className={`border-b border-r border-gray-200 p-2 min-h-[80px] relative group ${isHolidayCol ? 'bg-amber-50 pointer-events-none' : 'bg-white'}`}
                    >
                      {/* Course badge */}
                      <span className={`inline-block px-1.5 py-0.5 rounded text-xs font-medium border ${colorClass}`}>
                        {slot.originalCourseName ? (
                          <>
                            <span className="line-through text-gray-400 mr-1">{slot.originalCourseName}</span>
                            <span className="font-semibold text-brand-700">{slot.courseName}</span>
                          </>
                        ) : (
                          slot.courseName
                        )}
                      </span>

                      {/* Beskrivelse */}
                      {slot.beskrivelse && (
                        <p className="text-xs text-gray-700 line-clamp-2 mt-1">{slot.beskrivelse}</p>
                      )}

                      {/* Lektier indicator */}
                      {slot.lektier && (
                        <div className="flex items-center gap-1 mt-1">
                          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-amber-600 shrink-0">
                            <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                          </svg>
                          <span className="text-xs text-amber-700 line-clamp-1">{slot.lektier}</span>
                        </div>
                      )}

                      {/* Files indicator */}
                      {slot.files.length > 0 && (
                        <div className="flex items-center gap-1 mt-1">
                          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-gray-400 shrink-0">
                            <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
                          </svg>
                          <span className="text-xs text-gray-500">{slot.files.length}</span>
                        </div>
                      )}

                      {/* Edit button */}
                      <button
                        onClick={() => setEditingSlot(slot)}
                        className="opacity-0 group-hover:opacity-100 transition-opacity absolute bottom-1 right-1 p-1 rounded text-gray-400 hover:text-gray-700"
                        title="Rediger lektion"
                      >
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                          <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                        </svg>
                      </button>
                    </div>
                  )
                })}
              </>
            ))}
          </div>
        )}

        {!isLoading && weekPlanData?.slots.length === 0 && weekPlanData?.breakSlots.length === 0 && (
          <div className="p-8 text-center text-gray-400 text-sm">
            Ingen aktivt skema for denne klasse — opret et skema og sæt en datoperiode under Klasser.
          </div>
        )}
      </div>

      {/* Edit modal */}
      {editingSlot && classId && (
        <EditSlotModal
          slot={editingSlot}
          classId={classId}
          isoYear={isoYear}
          isoWeek={isoWeek}
          weekdayLabel={WEEKDAYS[WEEKDAY_NUMS.indexOf(editingSlot.weekday)] ?? ''}
          courses={courses ?? []}
          files={files ?? []}
          onClose={() => setEditingSlot(null)}
        />
      )}
    </div>
  )
}
