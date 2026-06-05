/**
 * Detect klassetrin (0–9) from a class name.
 * 0 = børnehaveklasse, 1–9 = 1.–9. klasse.
 * Returns null if no leading digit in range [0, 9] is found.
 *
 * Handles: "0.a", "1.a", "9.b", "0B", "2-A", "3 a"
 */
export function detectGradeLevel(name: string): number | null {
  const trimmed = name.trim()
  const m = trimmed.match(/^(\d+)[.\-_\s]/i) ?? trimmed.match(/^(\d+)(?=[A-Za-z])/i)
  if (!m) return null
  const n = parseInt(m[1], 10)
  return n >= 0 && n <= 9 ? n : null
}

export const GRADE_LEVEL_LABELS: Record<number, string> = {
  0: 'Børnehaveklasse (0.)',
  1: '1. klasse',
  2: '2. klasse',
  3: '3. klasse',
  4: '4. klasse',
  5: '5. klasse',
  6: '6. klasse',
  7: '7. klasse',
  8: '8. klasse',
  9: '9. klasse',
  10: '10. klasse',
}
