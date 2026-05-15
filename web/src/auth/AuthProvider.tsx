import { useEffect, useState, type ReactNode } from 'react'
import keycloak, { getInitPromise } from './keycloak'
import { AuthContext } from './AuthContext'
import type { StaffRole } from '../api/generated/types.gen'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authenticated, setAuthenticated] = useState(false)
  const [initialized, setInitialized] = useState(false)
  const [staffRole, setStaffRole] = useState<StaffRole | null>(null)
  const [staffId, setStaffId] = useState<string | null>(null)

  useEffect(() => {
    getInitPromise()
      .then((auth) => {
        setAuthenticated(auth)
        setInitialized(true)

        if (!auth) return

        // Proactively refresh token before it expires (30s before expiry)
        setInterval(() => {
          keycloak.updateToken(30).catch(() => {
            if (document.visibilityState === 'visible') {
              keycloak.login()
            }
          })
        }, 60_000)

        // On mobile, tabs suspend and tokens expire silently. Force refresh when tab becomes visible.
        const handleVisibilityChange = () => {
          if (document.visibilityState === 'visible') {
            keycloak.updateToken(300).catch(() => keycloak.login())
          }
        }
        document.addEventListener('visibilitychange', handleVisibilityChange)
        return () => document.removeEventListener('visibilitychange', handleVisibilityChange)
      })
      .catch(() => {
        setInitialized(true)
      })
  }, [])

  useEffect(() => {
    if (!authenticated || !keycloak.token) return

    fetch('/api/v1/staff/me', {
      headers: { Authorization: `Bearer ${keycloak.token}` },
    })
      .then((res) => (res.ok ? res.json() : null))
      .then((data: { id?: string; role?: StaffRole } | null) => {
        if (data) {
          setStaffRole(data.role ?? null)
          setStaffId(data.id ?? null)
        }
      })
      .catch(() => {
        // non-critical; leave null
      })
  }, [authenticated])

  if (!initialized) {
    return null
  }

  const parsed = keycloak.tokenParsed as Record<string, unknown> | undefined
  const nameRaw = parsed?.['name']
  const preferredRaw = parsed?.['preferred_username']
  const userName = typeof nameRaw === 'string' ? nameRaw : typeof preferredRaw === 'string' ? preferredRaw : undefined
  const realmAccess = parsed?.['realm_access']
  const rawRoles = (realmAccess !== null && typeof realmAccess === 'object' && !Array.isArray(realmAccess))
    ? (realmAccess as Record<string, unknown>)['roles']
    : undefined
  // UI-only: used for display/UI hints only. Server enforces actual authorization.
  const roles = Array.isArray(rawRoles) ? rawRoles.filter((r): r is string => typeof r === 'string') : []
  const isAdmin = roles.includes('admin')

  return (
    <AuthContext.Provider
      value={{
        authenticated,
        isAdmin,
        staffRole,
        staffId,
        token: keycloak.token,
        userName,
        logout: () => keycloak.logout({ redirectUri: 'https://skoleoverblikket.dk' }),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
