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

  const parsed = keycloak.tokenParsed as Record<string, string> | undefined
  const userName =
    parsed?.['name'] ??
    parsed?.['preferred_username'] ??
    undefined

  return (
    <AuthContext.Provider
      value={{
        authenticated,
        token: keycloak.token,
        userName,
        logout: () => keycloak.logout({ redirectUri: 'https://skoleoverblikket.dk' }),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
