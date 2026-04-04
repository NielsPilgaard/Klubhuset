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

        // Proactively refresh token before it expires (30s before expiry)
        setInterval(() => {
          keycloak.updateToken(30).catch(() => {
            keycloak.login()
          })
        }, 60_000)
      })
      .catch(() => {
        keycloak.login()
      })
  }, [])

  if (!initialized) {
    return null
  }

  const parsed = keycloak.tokenParsed as Record<string, string> | undefined
  const userName =
    parsed?.['name'] ??
    parsed?.['preferred_username'] ??
    null

  return (
    <AuthContext.Provider
      value={{
        authenticated,
        token: keycloak.token,
        userName,
        logout: () => keycloak.logout(),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
