import type { BillingInterval } from '../api/client'

export const BILLING_INTERVAL_STORAGE_KEY = 'skoleoverblikket:signup-billing-interval'

export function parseBillingInterval(value: string | null): BillingInterval | null {
  return value === 'Monthly' || value === 'Yearly' ? value : null
}
