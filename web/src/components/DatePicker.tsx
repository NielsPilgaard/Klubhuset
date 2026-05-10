import { useState, useRef, useEffect } from 'react'
import { DayPicker } from 'react-day-picker'
import { da } from 'react-day-picker/locale'

interface DatePickerProps {
  value: string // ISO yyyy-MM-dd
  onChange: (value: string) => void
  min?: string // ISO yyyy-MM-dd — dates before this are disabled
  label?: string
  placeholder?: string
}

function isoToDate(iso: string): Date {
  return new Date(`${iso}T00:00:00`)
}

function dateToIso(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function formatDDMMYYYY(iso: string): string {
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

export function DatePicker({ value, onChange, min, placeholder = 'Vælg dato' }: DatePickerProps) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  const selected = value ? isoToDate(value) : undefined
  const disabled = min ? { before: isoToDate(min) } : undefined

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    if (open) document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [open])

  function handleSelect(day: Date | undefined) {
    if (day) {
      onChange(dateToIso(day))
      setOpen(false)
    }
  }

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="w-full flex items-center gap-2 px-3 py-2 border border-gray-300 rounded-lg text-sm text-left focus:outline-none focus:ring-2 focus:ring-brand-500 hover:border-gray-400 bg-white transition-colors"
      >
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          className="text-gray-400 flex-shrink-0"
        >
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
          <line x1="16" y1="2" x2="16" y2="6" />
          <line x1="8" y1="2" x2="8" y2="6" />
          <line x1="3" y1="10" x2="21" y2="10" />
        </svg>
        <span className={value ? 'text-gray-900' : 'text-gray-400'}>
          {value ? formatDDMMYYYY(value) : placeholder}
        </span>
      </button>

      {open && (
        <div className="absolute z-50 top-full left-0 mt-1 bg-white border border-gray-200 rounded-xl shadow-xl p-3 w-fit">
          <DayPicker
            mode="single"
            locale={da}
            selected={selected}
            onSelect={handleSelect}
            disabled={disabled}
            defaultMonth={selected ?? (min ? isoToDate(min) : undefined)}
            classNames={{
              root: 'text-sm',
              months: 'flex',
              month: 'w-64',
              month_caption: 'flex items-center justify-between px-1 mb-3',
              caption_label: 'font-semibold text-gray-900 capitalize',
              nav: 'flex items-center gap-1',
              button_previous:
                'p-1.5 rounded-lg hover:bg-gray-100 text-gray-600 transition-colors disabled:opacity-30',
              button_next:
                'p-1.5 rounded-lg hover:bg-gray-100 text-gray-600 transition-colors disabled:opacity-30',
              weeks: 'w-full',
              week: 'grid grid-cols-7',
              weekdays: 'grid grid-cols-7 mb-1',
              weekday: 'text-xs text-center text-gray-400 font-medium py-1',
              day: '',
              day_button:
                'h-9 w-full rounded-lg text-sm font-medium transition-colors text-gray-700 hover:bg-gray-100',
              today: 'border border-brand-400 text-brand-700 hover:bg-brand-50',
              selected: '!bg-brand-600 !text-white hover:!bg-brand-700',
              disabled: '!text-gray-300 !cursor-not-allowed !hover:bg-transparent',
              outside: 'text-gray-300',
              range_start: '',
              range_end: '',
              range_middle: '',
            }}
          />
        </div>
      )}
    </div>
  )
}
