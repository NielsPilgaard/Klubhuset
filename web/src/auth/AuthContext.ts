import { createContext } from 'react'
import type { StaffRole } from '../api/generated/types.gen'

export interface AuthContextValue {
  authenticated: boolean
  isAdmin: boolean
  isParent: boolean
  isSuperAdmin: boolean
  staffRole: StaffRole | null
  staffId: string | null
  token: string | undefined
  userName: string | undefined
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
