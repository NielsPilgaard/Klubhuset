import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import keycloak from './keycloak'

interface AuthContextValue {
  authenticated: boolean
  token: string | undefined
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authenticated, setAuthenticated] = useState(false)
  const [initialized, setInitialized] = useState(false)

  useEffect(() => {
    keycloak
      .init({
        onLoad: 'login-required',
        pkceMethod: 'S256',
        checkLoginIframe: false,
      })
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

  return (
    <AuthContext.Provider
      value={{
        authenticated,
        token: keycloak.token,
        logout: () => keycloak.logout(),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
