import React, { useState, useRef, useEffect } from 'react'
import { Modal } from '../components/Modal'
import { useParams, useSearchParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getApiV1ClassesByClassIdUgeplanOptions,
  getApiV1ClassesByClassIdUgeplanQueryKey,
  putApiV1ClassesByClassIdUgeplanSlotsMutation,
  postApiV1ClassesByClassIdUgeplanSlotsBySlotIdFilesMutation,
  deleteApiV1ClassesByClassIdUgeplanSlotsBySlotIdFilesByFileIdMutation,
  getApiV1CoursesOptions,
  getApiV1ClassesOptions,
} from '../api/generated/@tanstack/react-query.gen'
import type { ClassDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'
import { FilePicker } from '../components/files/FilePicker'

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
  weekday: 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday'
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
  weekday: 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday'
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

// ─── ISO week helpers ─────────────────────────────────────────────────────────

function getISOWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7)
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
    const label = y !== startWeekYear || w === 1 ? `Uge ${w}, ${y}` : `Uge ${w}`
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

import { WEEKDAY_LABELS as WEEKDAYS, WEEKDAY_KEYS } from '../lib/weekdays'

// ─── Edit modal ───────────────────────────────────────────────────────────────

interface EditSlotModalProps {
  slot: WeekPlanSlotDto
  classId: string
  isoYear: number
  isoWeek: number
  schemaId: string | null
  weekdayLabel: string
  courses: CourseDto[]
  onClose: () => void
}

const AUTOSAVE_PREFIX = 'ugeplan_draft_'

function autosaveKey(schemaSlotId: string) {
  return `${AUTOSAVE_PREFIX}${schemaSlotId}`
}

function EditSlotModal({
  slot,
  classId,
  isoYear,
  isoWeek,
  schemaId,
  weekdayLabel,
  courses,
  onClose,
}: EditSlotModalProps) {
  const qc = useQueryClient()

  const savedDraft = (() => {
    try {
      return JSON.parse(sessionStorage.getItem(autosaveKey(slot.schemaSlotId)) ?? 'null')
    } catch {
      return null
    }
  })()

  const [beskrivelse, setBeskrivelse] = useState(savedDraft?.beskrivelse ?? slot.beskrivelse ?? '')
  const [lektier, setLektier] = useState(savedDraft?.lektier ?? slot.lektier ?? '')
  const [fagSwapCourseId, setFagSwapCourseId] = useState(slot.originalCourseId ? slot.courseId : '')
  const [justSaved, setJustSaved] = useState(false)
  const autosaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    if (autosaveTimer.current) clearTimeout(autosaveTimer.current)
    autosaveTimer.current = setTimeout(() => {
      sessionStorage.setItem(
        autosaveKey(slot.schemaSlotId),
        JSON.stringify({ beskrivelse, lektier })
      )
    }, 1000)
    return () => {
      if (autosaveTimer.current) clearTimeout(autosaveTimer.current)
    }
  }, [beskrivelse, lektier, slot.schemaSlotId])

  const ugeplanQueryKey = getApiV1ClassesByClassIdUgeplanQueryKey({
    path: { classId },
    query: { isoYear, isoWeek, ...(schemaId ? { schemaId } : {}) },
  })

  async function ensureSlotSaved(): Promise<string> {
    if (slot.id !== '00000000-0000-0000-0000-000000000000') return slot.id
    const { mutationFn } = putApiV1ClassesByClassIdUgeplanSlotsMutation()
    const updated = await mutationFn!(
      {
        path: { classId },
        query: { isoYear, isoWeek, ...(schemaId ? { schemaId } : {}) },
        body: {
          schemaSlotId: slot.schemaSlotId,
          beskrivelse: beskrivelse || null,
          lektier: lektier || null,
          fagSwapCourseId: fagSwapCourseId || null,
        },
      },
      undefined as never
    )
    qc.setQueryData(ugeplanQueryKey, (old: WeekPlanDto | undefined) => {
      if (!old) return old
      return {
        ...old,
        slots: old.slots.map((s) =>
          s.schemaSlotId === (updated as WeekPlanSlotDto).schemaSlotId
            ? { ...s, ...(updated as WeekPlanSlotDto) }
            : s
        ),
      }
    })
    return (updated as WeekPlanSlotDto).id
  }

  const upsertMutation = useMutation({
    ...putApiV1ClassesByClassIdUgeplanSlotsMutation(),
    onSuccess: (updated) => {
      qc.setQueryData(ugeplanQueryKey, (old: WeekPlanDto | undefined) => {
        if (!old) return old
        return {
          ...old,
          slots: old.slots.map((s) =>
            s.schemaSlotId === (updated as WeekPlanSlotDto).schemaSlotId
              ? { ...s, ...(updated as WeekPlanSlotDto) }
              : s
          ),
        }
      })
      sessionStorage.removeItem(autosaveKey(slot.schemaSlotId))
      setJustSaved(true)
      setTimeout(() => setJustSaved(false), 2000)
    },
  })

  const addFileMutation = useMutation({
    ...postApiV1ClassesByClassIdUgeplanSlotsBySlotIdFilesMutation(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ugeplanQueryKey }),
  })

  const removeFileMutation = useMutation({
    ...deleteApiV1ClassesByClassIdUgeplanSlotsBySlotIdFilesByFileIdMutation(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ugeplanQueryKey }),
  })

  function handleSave() {
    if (upsertMutation.isPending) return
    upsertMutation.mutate({
      path: { classId },
      query: { isoYear, isoWeek, ...(schemaId ? { schemaId } : {}) },
      body: {
        schemaSlotId: slot.schemaSlotId,
        beskrivelse: beskrivelse || null,
        lektier: lektier || null,
        fagSwapCourseId: fagSwapCourseId || null,
      },
    })
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Escape') {
      onClose()
      return
    }
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault()
      handleSave()
    }
  }

  function handleTextareaKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      if (!upsertMutation.isPending) {
        upsertMutation.mutate(
          {
            path: { classId },
            query: { isoYear, isoWeek, ...(schemaId ? { schemaId } : {}) },
            body: {
              schemaSlotId: slot.schemaSlotId,
              beskrivelse: beskrivelse || null,
              lektier: lektier || null,
              fagSwapCourseId: fagSwapCourseId || null,
            },
          },
          {
            onSuccess: (updated) => {
              qc.setQueryData(ugeplanQueryKey, (old: WeekPlanDto | undefined) => {
                if (!old) return old
                return {
                  ...old,
                  slots: old.slots.map((s) =>
                    s.schemaSlotId === (updated as WeekPlanSlotDto).schemaSlotId
                      ? { ...s, ...(updated as WeekPlanSlotDto) }
                      : s
                  ),
                }
              })
              onClose()
            },
          }
        )
      }
    }
  }

  async function handleFileToggle(fileId: string, checked: boolean) {
    const slotId = await ensureSlotSaved()
    const existingLink = slot.files.find((f) => f.schoolFileId === fileId)
    if (checked && !existingLink) {
      addFileMutation.mutate({ path: { classId, slotId }, body: { schoolFileId: fileId } })
    } else if (!checked && existingLink) {
      removeFileMutation.mutate({ path: { classId, slotId, fileId: existingLink.id } })
    }
  }

  const selectedFileIds = slot.files.map((f) => f.schoolFileId)
  const isFileMutationPending = addFileMutation.isPending || removeFileMutation.isPending

  return (
    <Modal
      isOpen
      onClose={onClose}
      onKeyDown={handleKeyDown}
      backdropClassName="fixed inset-0 z-50 flex items-end sm:items-center justify-center sm:p-4 bg-black/40"
      contentClassName="bg-white rounded-t-2xl sm:rounded-2xl shadow-xl w-full sm:max-w-lg max-h-[92vh] sm:max-h-[90vh] overflow-y-auto"
    >
      <div className="px-6 py-5 border-b border-gray-100">
        <h2 className="font-display text-lg font-semibold text-gray-900">
          {slot.courseName} — {weekdayLabel}
        </h2>
        <p className="text-xs text-gray-400 mt-0.5">
          {slot.startTime.slice(0, 5)}–{slot.endTime.slice(0, 5)}
        </p>
      </div>

      <div className="px-6 py-5 space-y-5">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Beskrivelse</label>
          <textarea
            autoFocus
            rows={5}
            value={beskrivelse}
            onChange={(e) => setBeskrivelse(e.target.value)}
            onKeyDown={handleTextareaKeyDown}
            placeholder="Hvad skal der ske i denne lektion?"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent resize-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-blue-700 mb-1">Lektier</label>
          <textarea
            rows={4}
            value={lektier}
            onChange={(e) => setLektier(e.target.value)}
            onKeyDown={handleTextareaKeyDown}
            placeholder="Opgaver til næste gang..."
            className="w-full px-3 py-2 border border-blue-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-400 focus:border-transparent resize-none bg-blue-50/40"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Fagbytte</label>
          <select
            value={fagSwapCourseId}
            onChange={(e) => setFagSwapCourseId(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent bg-white"
          >
            <option value="">Intet fagbytte (brug skemaets fag)</option>
            {courses
              .filter((c) => c.id !== (slot.originalCourseId ?? slot.courseId))
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
          </select>
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-gray-700">Filer</label>
            <a
              href="/filer"
              target="_blank"
              rel="noopener noreferrer"
              className="text-xs text-gray-400 hover:text-gray-600"
            >
              Administrer filer ↗
            </a>
          </div>
          <FilePicker
            selectedFileIds={selectedFileIds}
            onToggle={handleFileToggle}
            disabled={isFileMutationPending}
          />
        </div>

        {upsertMutation.isError && (
          <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
        )}
      </div>

      <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between">
        <span className="text-xs text-gray-400">
          Enter for at gemme · Shift+Enter for linjeskift · Ctrl+S for at gemme
        </span>
        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
          >
            Luk
          </button>
          <button
            onClick={handleSave}
            disabled={upsertMutation.isPending}
            className={`px-4 py-2 text-sm rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed ${justSaved ? 'bg-green-600 text-white hover:bg-green-700' : 'bg-brand-600 text-white hover:bg-brand-700'}`}
          >
            {upsertMutation.isPending ? 'Gemmer...' : justSaved ? 'Gemt ✓' : 'Gem'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function WeekPlanPage() {
  usePageTitle('Ugeplan')
  const { classId } = useParams<{ classId: string }>()
  const [searchParams] = useSearchParams()
  const schemaId = searchParams.get('schemaId')
  const navigate = useNavigate()

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

  const { data: rawWeekPlanData, isLoading } = useQuery({
    ...getApiV1ClassesByClassIdUgeplanOptions({
      path: { classId: classId! },
      query: { isoYear, isoWeek, ...(schemaId ? { schemaId } : {}) },
    }),
    enabled: !!classId,
  })
  const weekPlanData = rawWeekPlanData as WeekPlanDto | undefined

  const { data: rawCourses } = useQuery(getApiV1CoursesOptions())
  const courses = (rawCourses ?? []) as CourseDto[]

  // Collect all unique course IDs for color assignment
  const allCourseIds =
    weekPlanData?.slots.map((s) => s.courseId).filter((v, i, a) => a.indexOf(v) === i) ?? []

  // Build a combined sorted list of rows: regular lesson slots + break slots
  type GridRow =
    | {
        kind: 'slot'
        timeSlotId: string
        timeSlotLabel: string
        startTime: string
        endTime: string
      }
    | {
        kind: 'break'
        timeSlotId: string
        timeSlotLabel: string
        startTime: string
        endTime: string
      }

  const uniqueTimeSlots: GridRow[] = (() => {
    if (!weekPlanData) return []
    const lessonRows: GridRow[] = weekPlanData.slots
      .filter((s, i, a) => a.findIndex((x) => x.timeSlotId === s.timeSlotId) === i)
      .map((s) => ({
        kind: 'slot' as const,
        timeSlotId: s.timeSlotId,
        timeSlotLabel: s.timeSlotLabel,
        startTime: s.startTime,
        endTime: s.endTime,
      }))
    const breakRows: GridRow[] = (weekPlanData.breakSlots ?? []).map((b) => ({
      kind: 'break' as const,
      timeSlotId: b.timeSlotId,
      timeSlotLabel: b.timeSlotLabel,
      startTime: b.startTime,
      endTime: b.endTime,
    }))
    return [...lessonRows, ...breakRows].sort((a, b) => a.startTime.localeCompare(b.startTime))
  })()

  // Parse weekStartDate to compute column dates
  const weekStartDate = weekPlanData?.weekStartDate
    ? new Date(weekPlanData.weekStartDate + 'T00:00:00')
    : null

  const { data: classData } = useQuery({
    ...getApiV1ClassesOptions(),
    select: (all) => (all ?? []).find((c) => c.id === classId) as ClassDto | undefined,
    enabled: !!classId,
  })

  const className = classData?.name ?? ''

  return (
    <div className="flex flex-col h-full">
      {/* Top bar */}
      <div className="shrink-0 sticky top-0 z-10 bg-white border-b border-gray-200 px-4 lg:px-6 py-3 flex items-center justify-between">
        <div className="flex items-center gap-2 min-w-0">
          <button
            onClick={() => navigate(-1)}
            className="text-gray-400 hover:text-gray-700 transition-colors shrink-0"
            aria-label="Tilbage"
          >
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
            >
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>
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
              <option key={`${y}-${w}`} value={`${y}-${w}`}>
                {label}
              </option>
            ))}
          </select>
          <button
            onClick={nextWeek}
            className="text-sm text-gray-600 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100 transition-colors"
          >
            →
          </button>
          <button onClick={goToThisWeek} className="text-xs text-brand-600 hover:underline ml-2">
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
          <div className="p-8 text-center text-gray-400 text-sm animate-pulse">
            Henter ugeplan...
          </div>
        )}

        {!isLoading && weekPlanData && (
          <div
            className={`grid grid-cols-[auto_1fr_1fr_1fr_1fr_1fr] min-w-0 ${weekPlanData.isHolidayWeek ? 'pointer-events-none opacity-60' : ''}`}
          >
            {/* Header row */}
            <div className="bg-gray-50 border-b border-r border-gray-200 p-2" />{' '}
            {/* empty corner */}
            {WEEKDAYS.map((label, i) => {
              const weekday = WEEKDAY_KEYS[i]
              const date = weekStartDate ? new Date(weekStartDate.getTime() + i * 86400000) : null
              const dateLabel = date
                ? date.toLocaleDateString('da-DK', { day: '2-digit', month: 'short' })
                : ''
              const holidayDay = (weekPlanData.holidayDays ?? []).find((h) => h.weekday === weekday)
              return (
                <div
                  key={label}
                  className={`border-b border-r border-gray-200 p-2 text-center ${holidayDay ? 'bg-amber-50' : 'bg-gray-50'}`}
                >
                  <div className="text-xs font-semibold text-gray-700">{label}</div>
                  {dateLabel && <div className="text-xs text-gray-400">{dateLabel}</div>}
                  {holidayDay && (
                    <div className="text-xs text-amber-600 font-medium mt-0.5 leading-tight">
                      {holidayDay.title}
                    </div>
                  )}
                </div>
              )
            })}
            {/* Data rows */}
            {uniqueTimeSlots.map((ts) => (
              <React.Fragment key={`row-${ts.timeSlotId}`}>
                {/* Time label */}
                <div
                  className={`border-b border-r border-gray-200 p-2 flex flex-col justify-center ${ts.kind === 'break' ? 'bg-gray-100' : 'bg-gray-50'}`}
                >
                  <span className="text-xs text-gray-500 font-mono whitespace-nowrap">
                    {ts.timeSlotLabel}
                  </span>
                  <span className="text-xs text-gray-400 font-mono">
                    {ts.startTime.slice(0, 5)}
                  </span>
                </div>

                {/* Break row: grey separator spanning all columns */}
                {ts.kind === 'break' &&
                  WEEKDAY_KEYS.map((_, dayIdx) => (
                    <div
                      key={`break-${ts.timeSlotId}-${dayIdx}`}
                      className="border-b border-r border-gray-200 bg-gray-100 min-h-[28px]"
                    />
                  ))}

                {/* Day cells (lesson rows only) */}
                {ts.kind === 'slot' &&
                  WEEKDAY_KEYS.map((dayKey, dayIdx) => {
                    const slot = weekPlanData.slots.find(
                      (s) => s.timeSlotId === ts.timeSlotId && s.weekday === dayKey
                    )
                    const isHolidayCol = (weekPlanData.holidayDays ?? []).some(
                      (h) => h.weekday === dayKey
                    )

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
                        onClick={() => setEditingSlot(slot)}
                        className={`border-b border-r border-gray-200 p-2 min-h-[80px] cursor-pointer transition-colors ${isHolidayCol ? 'bg-amber-50 pointer-events-none' : 'bg-white hover:bg-gray-50'}`}
                      >
                        {/* Course badge */}
                        <span
                          className={`inline-block px-1.5 py-0.5 rounded text-xs font-medium border ${colorClass}`}
                        >
                          {slot.originalCourseName ? (
                            <>
                              <span className="line-through text-gray-400 mr-1">
                                {slot.originalCourseName}
                              </span>
                              <span className="font-semibold text-brand-700">
                                {slot.courseName}
                              </span>
                            </>
                          ) : (
                            slot.courseName
                          )}
                        </span>

                        {/* Beskrivelse */}
                        {slot.beskrivelse && (
                          <p className="text-xs text-gray-700 line-clamp-3 mt-1 whitespace-pre-wrap">
                            {slot.beskrivelse}
                          </p>
                        )}

                        {/* Lektier indicator */}
                        {slot.lektier && (
                          <div className="flex items-start gap-1 mt-1">
                            <svg
                              width="11"
                              height="11"
                              viewBox="0 0 24 24"
                              fill="none"
                              stroke="currentColor"
                              strokeWidth="2"
                              className="text-blue-500 shrink-0 mt-0.5"
                            >
                              <path d="M12 20h9" />
                              <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                            </svg>
                            <span className="text-xs text-blue-700 line-clamp-2 whitespace-pre-wrap">
                              {slot.lektier}
                            </span>
                          </div>
                        )}

                        {/* Files indicator */}
                        {slot.files.length > 0 && (
                          <div className="flex items-center gap-1 mt-1">
                            <svg
                              width="11"
                              height="11"
                              viewBox="0 0 24 24"
                              fill="none"
                              stroke="currentColor"
                              strokeWidth="2"
                              className="text-gray-400 shrink-0"
                            >
                              <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
                            </svg>
                            <span className="text-xs text-gray-500">{slot.files.length}</span>
                          </div>
                        )}
                      </div>
                    )
                  })}
              </React.Fragment>
            ))}
          </div>
        )}

        {!isLoading &&
          weekPlanData?.slots.length === 0 &&
          weekPlanData?.breakSlots.length === 0 && (
            <div className="p-8 text-center text-gray-400 text-sm">
              Ingen aktivt skema for denne klasse — opret et skema og sæt en datoperiode under
              Klasser.
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
          schemaId={schemaId}
          weekdayLabel={WEEKDAYS[WEEKDAY_KEYS.indexOf(editingSlot.weekday)] ?? ''}
          courses={courses}
          onClose={() => setEditingSlot(null)}
        />
      )}
    </div>
  )
}
