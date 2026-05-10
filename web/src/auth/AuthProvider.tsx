import { useEffect, useState, type ReactNode } from 'react'
import keycloak, { getInitPromise } from './keycloak'
import { AuthContext } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authenticated, setAuthenticated] = useState(false)
  const [initialized, setInitialized] = useState(false)

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
      })
      .catch(() => {
        setInitialized(true)
      })
  }, [])

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
        token: keycloak.token,
        userName,
        logout: () => keycloak.logout({ redirectUri: 'https://skoleoverblikket.dk' }),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
