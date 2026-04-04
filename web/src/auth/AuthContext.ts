import { createContext } from 'react'

export interface AuthContextValue {
  authenticated: boolean
  token: string | undefined
  userName: string | null
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
