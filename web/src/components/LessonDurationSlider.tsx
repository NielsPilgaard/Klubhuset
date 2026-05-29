interface LessonDurationSliderProps {
  value: number
  onChange: (value: number) => void
  'data-testid'?: string
}

export function LessonDurationSlider({
  value,
  onChange,
  'data-testid': testId,
}: LessonDurationSliderProps) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        Lektionslængde — {value} minutter
      </label>
      <input
        type="range"
        min={20}
        max={120}
        step={5}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-full accent-brand-600"
        data-testid={testId}
      />
      <div className="flex justify-between text-xs text-gray-400 mt-0.5">
        <span>20 min</span>
        <span>120 min</span>
      </div>
    </div>
  )
}
