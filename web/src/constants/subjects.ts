import type { SubjectCategory } from '../api/generated/types.gen'

export const SUBJECT_CATEGORY_LABELS: Record<SubjectCategory, string> = {
  Dansk: 'Dansk',
  Matematik: 'Matematik',
  Engelsk: 'Engelsk',
  Naturfag: 'Naturfag',
  Historie: 'Historie',
  Musik: 'Musik',
  Idraet: 'Idræt',
  Kristendomskundskab: 'Kristendomskundskab',
  Billedkunst: 'Billedkunst',
  HaandvaerkOgDesign: 'Håndværk og design',
  Tysk: 'Tysk',
  Fransk: 'Fransk',
  Geografi: 'Geografi',
  Biologi: 'Biologi',
  FysikKemi: 'Fysik/kemi',
  Samfundsfag: 'Samfundsfag',
  Fri: 'Fri/brugerdefineret',
}

export const ALL_SUBJECT_CATEGORIES = Object.keys(SUBJECT_CATEGORY_LABELS) as SubjectCategory[]
