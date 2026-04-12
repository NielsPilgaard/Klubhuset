export interface StandardCourse {
  name: string
  color: string
}

export const STANDARD_COURSES: StandardCourse[] = [
  { name: 'Dansk',               color: '#3b82f6' }, // blue
  { name: 'Matematik',           color: '#f97316' }, // orange
  { name: 'Engelsk',             color: '#8b5cf6' }, // purple
  { name: 'Naturfag',            color: '#10b981' }, // emerald
  { name: 'Historie',            color: '#f59e0b' }, // amber
  { name: 'Musik',               color: '#ec4899' }, // pink
  { name: 'Idræt',               color: '#06b6d4' }, // cyan
  { name: 'Kristendom',          color: '#6366f1' }, // indigo
  { name: 'Billedkunst',         color: '#f43f5e' }, // rose
  { name: 'Håndværk og design',  color: '#84cc16' }, // lime
  { name: 'Tysk',                color: '#14b8a6' }, // teal
  { name: 'Fransk',              color: '#eab308' }, // yellow
  { name: 'Geografi',            color: '#a16207' }, // brown-ish
  { name: 'Biologi',             color: '#16a34a' }, // green
  { name: 'Fysik/kemi',          color: '#7c3aed' }, // violet
  { name: 'Samfundsfag',         color: '#dc2626' }, // red
]
