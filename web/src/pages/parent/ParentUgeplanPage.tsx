import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  getApiV1ClassesByClassIdUgeplanOptions,
} from '../../api/generated/@tanstack/react-query.gen'
import { getApiV1ParentsMe } from '../../api/generated/sdk.gen'
import type { ParentMeDto } from '../../api/client'
import { usePageTitle } from '../../hooks/usePageTitle'

const WEEKDAYS_DA: Record<string, string> = {
  Monday: 'Mandag',
  Tuesday: 'Tirsdag',
  Wednesday: 'Onsdag',
  Thursday: 'Torsdag',
  Friday: 'Fredag',
}

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
  // A year has 53 ISO weeks if Jan 1 or Dec 31 falls on Thursday
  const jan1 = new Date(Date.UTC(year, 0, 1)).getUTCDay()
  const dec31 = new Date(Date.UTC(year, 11, 31)).getUTCDay()
  return jan1 === 4 || dec31 === 4 ? 53 : 52
}

interface Slot {
  id: string
  weekday: string
  timeSlotLabel: string
  startTime: string
  courseName: string
  beskrivelse?: string | null
  lektier?: string | null
}

function ClassWeekPlan({ classId, className, isoYear, isoWeek }: { classId: string; className: string; isoYear: number; isoWeek: number }) {
  const { data, isLoading, isError } = useQuery(
    getApiV1ClassesByClassIdUgeplanOptions({ path: { classId }, query: { isoYear, isoWeek } })
  )

  if (isLoading) return <div className="text-sm text-gray-400">Indlæser ugeplan...</div>
  if (isError) return <div className="text-sm text-red-500">Fejl ved hentning af ugeplan.</div>

  const plan = data as { isHolidayWeek?: boolean; holidayTitle?: string | null; slots?: Slot[] } | undefined
  if (!plan) return null

  if (plan.isHolidayWeek) {
    return (
      <div>
        <h2 className="text-base font-semibold text-gray-900 mb-2">{className}</h2>
        <div className="p-3 bg-blue-50 text-blue-700 text-sm rounded-lg">{plan.holidayTitle ?? 'Ferie'}</div>
      </div>
    )
  }

  const slots = plan.slots ?? []
  const byDay = slots.reduce<Record<string, Slot[]>>((acc, s) => {
    const day = s.weekday ?? 'Monday'
    if (!acc[day]) acc[day] = []
    acc[day].push(s)
    return acc
  }, {})

  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']

  return (
    <div>
      <h2 className="text-base font-semibold text-gray-900 mb-3">{className}</h2>
      <div className="space-y-3">
        {days.map((day) => {
          const daySlots = byDay[day] ?? []
          if (daySlots.length === 0) return null
          return (
            <div key={day}>
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">
                {WEEKDAYS_DA[day] ?? day}
              </h3>
              <div className="space-y-1.5">
                {daySlots.map((s) => (
                  <div key={s.id} className="bg-white border border-gray-100 rounded-lg px-3 py-2">
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-gray-400 w-16 shrink-0">{s.startTime}</span>
                      <span className="text-sm font-medium text-gray-900">{s.courseName}</span>
                    </div>
                    {s.beskrivelse && (
                      <p className="mt-1 text-xs text-gray-600 ml-18">{s.beskrivelse}</p>
                    )}
                    {s.lektier && (
                      <p className="mt-1 text-xs text-amber-700 ml-18">
                        <span className="font-medium">Lektier: </span>{s.lektier}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )
        })}
        {slots.length === 0 && (
          <p className="text-sm text-gray-400">Ingen lektioner denne uge.</p>
        )}
      </div>
    </div>
  )
}

export default function ParentUgeplanPage() {
  usePageTitle('Ugeplan')

  const now = new Date()
  const [isoYear, setIsoYear] = useState(getISOWeekYear(now))
  const [isoWeek, setIsoWeek] = useState(getISOWeek(now))

  const { data: meRes, isLoading, isError } = useQuery({
    queryKey: ['parents', 'me'],
    queryFn: () => getApiV1ParentsMe({ throwOnError: false }),
  })

  function prevWeek() {
    if (isoWeek === 1) {
      const prevYear = isoYear - 1
      setIsoYear(prevYear)
      setIsoWeek(getISOWeeksInYear(prevYear))
    } else {
      setIsoWeek(w => w - 1)
    }
  }

  function nextWeek() {
    if (isoWeek >= getISOWeeksInYear(isoYear)) {
      setIsoYear(y => y + 1)
      setIsoWeek(1)
    } else {
      setIsoWeek(w => w + 1)
    }
  }

  if (isLoading) return <div className="p-6 text-sm text-gray-500">Indlæser...</div>
  if (isError) return <div className="p-6 text-sm text-red-600">Noget gik galt. Prøv igen senere.</div>

  const notFound = meRes?.response.status === 404
  const me = meRes?.data as ParentMeDto | undefined
  const classes = notFound ? [] : (me?.classes ?? [])

  return (
    <div className="p-4 md:p-6 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">Ugeplan</h1>
        <div className="flex items-center gap-2">
          <button
            onClick={prevWeek}
            className="p-1.5 rounded-md text-gray-500 hover:bg-gray-100"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="15 18 9 12 15 6" />
            </svg>
          </button>
          <span className="text-sm font-medium text-gray-700">Uge {isoWeek}, {isoYear}</span>
          <button
            onClick={nextWeek}
            className="p-1.5 rounded-md text-gray-500 hover:bg-gray-100"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="9 18 15 12 9 6" />
            </svg>
          </button>
        </div>
      </div>

      {classes.length === 0 ? (
        <p className="text-sm text-gray-500">
          Din konto er endnu ikke tilknyttet nogen klasser. Kontakt skolen, hvis du mener dette er en fejl.
        </p>
      ) : (
        <div className="space-y-8">
          {classes.map((c) => (
            <ClassWeekPlan
              key={c.classId}
              classId={c.classId ?? ''}
              className={c.className ?? ''}
              isoYear={isoYear}
              isoWeek={isoWeek}
            />
          ))}
        </div>
      )}
    </div>
  )
}
