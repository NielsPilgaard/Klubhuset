import { useContext } from 'react'
import { AuthContext, type AuthContextValue, type ViewAs } from './AuthContext'

export type { ViewAs }

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
