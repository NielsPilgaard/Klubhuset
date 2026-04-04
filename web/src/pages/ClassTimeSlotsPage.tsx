import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, TimeSlotDto, ClassDto, TemplateDto } from '../api/client'
import { usePageTitle } from '../hooks/usePageTitle'

interface EditableSlot {
  sortOrder: number
  startTime: string
  endTime: string
  label: string
}

function toEditable(slot: TimeSlotDto): EditableSlot {
  return {
    sortOrder: slot.sortOrder ?? 0,
    startTime: slot.startTime?.slice(0, 5) ?? '',
    endTime: slot.endTime?.slice(0, 5) ?? '',
    label: slot.label ?? '',
  }
}

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

  const { data: template } = useQuery<TemplateDto>({
    queryKey: ['time-slot-template'],
    queryFn: () => api.get('/time-slot-template'),
  })

  const [slots, setSlots] = useState<EditableSlot[]>([])
  const [dirty, setDirty] = useState(false)

  useEffect(() => {
    if (timeSlots) {
      setSlots(timeSlots.map(toEditable))
      setDirty(false)
    }
  }, [timeSlots])

  const saveMutation = useMutation({
    mutationFn: () =>
      api.put<TimeSlotDto[]>(`/classes/${classId}/time-slots`, slots.map((s, i) => ({
        sortOrder: i + 1,
        startTime: s.startTime + ':00',
        endTime: s.endTime + ':00',
        label: s.label || null,
      }))),
    onSuccess: (data) => {
      qc.setQueryData(['time-slots', classId], data)
      setSlots(data.map(toEditable))
      setDirty(false)
    },
  })

  const resetMutation = useMutation({
    mutationFn: () =>
      api.put<TimeSlotDto[]>(`/classes/${classId}/time-slots`, []),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['time-slots', classId] })
      setDirty(false)
    },
  })

  function updateSlot(idx: number, field: keyof EditableSlot, value: string) {
    setSlots((prev) => {
      const next = [...prev]
      next[idx] = { ...next[idx], [field]: value }
      return next
    })
    setDirty(true)
  }

  function addSlot() {
    const last = slots[slots.length - 1]
    const newStart = last?.endTime ?? '08:00'
    const [h, m] = newStart.split(':').map(Number)
    const dur = template?.lessonDurationMinutes ?? 45
    const endTotal = h * 60 + m + dur
    const newEnd = `${String(Math.floor(endTotal / 60)).padStart(2, '0')}:${String(endTotal % 60).padStart(2, '0')}`
    setSlots((prev) => [
      ...prev,
      { sortOrder: prev.length + 1, startTime: newStart, endTime: newEnd, label: '' },
    ])
    setDirty(true)
  }

  function removeSlot(idx: number) {
    setSlots((prev) => prev.filter((_, i) => i !== idx))
    setDirty(true)
  }

  const isCustom = timeSlots?.some((s) => s.classId != null)

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
        <div className="flex items-center gap-2">
          {isCustom && (
            <button
              onClick={() => resetMutation.mutate()}
              disabled={resetMutation.isPending}
              className="px-3 py-1.5 text-xs font-medium text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
            >
              {resetMutation.isPending ? 'Nulstiller...' : 'Nulstil til skolens standard'}
            </button>
          )}
          <button
            onClick={() => saveMutation.mutate()}
            disabled={!dirty || saveMutation.isPending}
            className="px-3 py-1.5 text-xs font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {saveMutation.isPending ? 'Gemmer...' : 'Gem ændringer'}
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-auto p-4 lg:p-6">
        {isLoading ? (
          <div className="animate-pulse space-y-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 bg-gray-100 rounded-lg" />
            ))}
          </div>
        ) : (
          <div className="max-w-lg space-y-2">
            {!isCustom && (
              <p className="text-sm text-gray-500 mb-4">
                Denne klasse bruger skolens standard lektionsstruktur. Rediger herunder for at tilpasse.
              </p>
            )}

            {slots.map((slot, idx) => (
              <div key={idx} className="flex items-center gap-3 p-3 bg-white border border-gray-200 rounded-lg">
                <span className="text-xs font-medium text-gray-400 w-5 text-right shrink-0">{idx + 1}.</span>
                <input
                  type="time"
                  value={slot.startTime}
                  onChange={(e) => updateSlot(idx, 'startTime', e.target.value)}
                  className="px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent tabular-nums"
                />
                <span className="text-gray-400 text-sm">–</span>
                <input
                  type="time"
                  value={slot.endTime}
                  onChange={(e) => updateSlot(idx, 'endTime', e.target.value)}
                  className="px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent tabular-nums"
                />
                <input
                  type="text"
                  value={slot.label}
                  onChange={(e) => updateSlot(idx, 'label', e.target.value)}
                  placeholder="Label (valgfrit)"
                  className="flex-1 min-w-0 px-2 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                />
                <button
                  onClick={() => removeSlot(idx)}
                  className="p-1 text-gray-300 hover:text-red-500 rounded transition-colors shrink-0"
                  title="Fjern lektion"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                </button>
              </div>
            ))}

            <button
              onClick={addSlot}
              className="w-full py-2 border-2 border-dashed border-gray-200 rounded-lg text-sm text-gray-400 hover:border-brand-400 hover:text-brand-500 transition-colors flex items-center justify-center gap-1.5"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="12" y1="5" x2="12" y2="19" />
                <line x1="5" y1="12" x2="19" y2="12" />
              </svg>
              Tilføj lektion
            </button>

            {(saveMutation.isError || resetMutation.isError) && (
              <p className="text-sm text-red-600 mt-2">Der opstod en fejl. Prøv igen.</p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
