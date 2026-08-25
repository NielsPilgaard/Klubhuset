const PHONE_PATTERN = /^(\+45)?\d{8}$/
const POSTAL_CODE_PATTERN = /^\d{4}$/

export function normalizePhone(phone: string): string {
  return phone.replace(/\s+/g, '')
}

export function isValidPhone(phone: string): boolean {
  const normalized = normalizePhone(phone)
  return normalized === '' || PHONE_PATTERN.test(normalized)
}

export function isValidPostalCode(postalCode: string): boolean {
  const trimmed = postalCode.trim()
  return trimmed === '' || POSTAL_CODE_PATTERN.test(trimmed)
}

export const PHONE_ERROR_MESSAGE = 'Telefonnummer skal være 8 cifre, evt. med +45 foran.'
export const POSTAL_CODE_ERROR_MESSAGE = 'Postnummer skal være 4 cifre.'
