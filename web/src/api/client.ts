import keycloak from '../auth/keycloak'
import { client } from './generated/client.gen'

export const API_BASE = '/api/v1'

// Configure the generated client with Keycloak bearer auth and the correct base URL.
client.setConfig({
  baseUrl: '',
  auth: async () => {
    await keycloak.updateToken(30).catch(() => keycloak.login())
    return keycloak.token
  },
})

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
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

async function requestForm<T>(path: string, body: FormData): Promise<T> {
  await keycloak.updateToken(30).catch(() => keycloak.login())

  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: keycloak.token ? { Authorization: `Bearer ${keycloak.token}` } : {},
    body,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new ApiError(res.status, text)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) => request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  postForm: <T>(path: string, body: FormData) => requestForm<T>(path, body),
  put: <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) => request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: (path: string) => request<void>(path, { method: 'DELETE' }),
}

// Re-export all generated types
export type {
  BreakDto,
  CalendarEntryDto,
  CalendarEntryType,
  ClassDto,
  ConflictInfo,
  ConflictType,
  CourseDto,
  DashboardStats,
  FileDto,
  RoomDto,
  SchemaDetailDto,
  SchemaDto,
  SlotDto,
  SlotsAndConflictsDto,
  StaffDto,
  StaffRole,
  TemplateDto,
  TimeSlotDto,
  WeekPlanDto,
  WeekPlanSlotDto,
  WeekPlanSlotFileDto,
  ScheduleSlotDto,
  OnboardingStatusDto,
  SchoolSettingsDto,
  SubscriptionDto,
  SubscriptionStatus,
  InvitationDto,
} from './generated/types.gen'
