export const API_BASE = '/api/v1'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = sessionStorage.getItem('access_token')
  const res = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
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

// ── Types mirroring API DTOs ───────────────────────────────────────────────

export type ClassDto = { id: string; name: string; description: string | null }
export type CourseDto = { id: string; name: string; description: string | null }
export type RoomDto = { id: string; name: string; capacity: number | null; description: string | null }
export type StaffRole = 'Teacher' | 'Aide' | 'Substitute'
export type StaffDto = { id: string; name: string; email: string | null; phone: string | null; role: StaffRole }
export type SchemaStatus = 'Draft' | 'Complete'
export type SchemaDto = { id: string; classId: string; name: string; status: SchemaStatus; isActive: boolean }

export type SlotDto = {
  id: string; timeSlotId: string; weekday: number
  courseId: string; courseName: string
  teacherId: string; teacherName: string
  roomId: string | null; roomName: string | null
  aideId: string | null; aideName: string | null
}

export type ConflictType = 'TeacherDoubleBooked' | 'RoomDoubleBooked' | 'AideDoubleBooked'
export type ConflictInfo = {
  type: ConflictType; slotAId: string; slotBId: string
  resourceId: string; resourceName: string
  weekday: number; startTime: string; endTime: string
}

export type SchemaDetailDto = { schema: SchemaDto; slots: SlotDto[]; conflicts: ConflictInfo[] }

export type TimeSlotDto = {
  id: string; classId: string | null; sortOrder: number
  startTime: string; endTime: string; label: string | null
}

export type DashboardStats = {
  classCount: number; staffCount: number; courseCount: number; roomCount: number
  schemasComplete: number; schemasTotal: number
  hoursPerCourse: { courseId: string; courseName: string; classId: string; className: string; hours: number }[]
  hoursPerStaff: { staffId: string; staffName: string; role: StaffRole; hours: number }[]
  unassignedClasses: { classId: string; className: string; emptySlots: number }[]
}
