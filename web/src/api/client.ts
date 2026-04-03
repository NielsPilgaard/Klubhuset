import keycloak from '../auth/keycloak'
import type { components } from './schema.d.ts'

export const API_BASE = '/api/v1'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  // Refresh token if it expires within the next 30 seconds
  await keycloak.updateToken(30).catch(() => keycloak.login())

  const res = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(keycloak.token ? { Authorization: `Bearer ${keycloak.token}` } : {}),
    },
    ...options,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new ApiError(res.status, text)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
  }
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) => request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (path: string) => request<void>(path, { method: 'DELETE' }),
}

// Re-export generated types for use across the app
export type ClassDto = components['schemas']['ClassDto']
export type CourseDto = components['schemas']['CourseDto']
export type RoomDto = components['schemas']['RoomDto']
export type StaffRole = components['schemas']['StaffRole']
export type StaffDto = components['schemas']['StaffDto']
export type SchemaStatus = components['schemas']['SchemaStatus']
export type SchemaDto = components['schemas']['SchemaDto']
export type SlotDto = components['schemas']['SlotDto']
export type ConflictType = components['schemas']['ConflictType']
export type ConflictInfo = components['schemas']['ConflictInfo']
export type SchemaDetailDto = components['schemas']['SchemaDetailDto']
export type SlotsAndConflictsDto = components['schemas']['SlotsAndConflictsDto']
export type TimeSlotDto = components['schemas']['TimeSlotDto']
export type DashboardStats = components['schemas']['DashboardStats']
export type TemplateDto = components['schemas']['TemplateDto']
export type BreakDto = components['schemas']['BreakDto']
