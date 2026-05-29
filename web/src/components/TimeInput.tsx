interface TimeInputProps {
  value: string // "HH:MM"
  onChange: (value: string) => void
  className?: string
}

export function TimeInput({ value, onChange, className = '' }: TimeInputProps) {
  const parts = value.split(':')
  const h = parts[0] ?? '08'
  const m = parts[1] ?? '00'

  return (
    <div className={`flex items-center gap-1 ${className}`}>
      <input
        type="number"
        min={0}
        max={23}
        value={parseInt(h, 10)}
        onFocus={(e) => e.target.select()}
        onChange={(e) => {
          const hh = String(Math.max(0, Math.min(23, Number(e.target.value)))).padStart(2, '0')
          onChange(`${hh}:${m}`)
        }}
        className="w-14 px-2 py-1.5 border border-gray-300 rounded-lg text-sm text-center focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
      />
      <span className="text-gray-400 font-medium select-none">:</span>
      <input
        type="number"
        min={0}
        max={59}
        step={5}
        value={parseInt(m, 10)}
        onFocus={(e) => e.target.select()}
        onChange={(e) => {
          const mm = String(Math.max(0, Math.min(59, Number(e.target.value)))).padStart(2, '0')
          onChange(`${h}:${mm}`)
        }}
        className="w-14 px-2 py-1.5 border border-gray-300 rounded-lg text-sm text-center focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
      />
    </div>
  )
}
