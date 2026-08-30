/**
 * Danish count-aware noun. Picks singular for exactly 1, plural otherwise.
 *
 *   pluralize(1, 'klasse', 'klasser') // '1 klasse'
 *   pluralize(3, 'klasse', 'klasser') // '3 klasser'
 */
export function pluralize(count: number, singular: string, plural: string): string {
  return `${count} ${count === 1 ? singular : plural}`
}
