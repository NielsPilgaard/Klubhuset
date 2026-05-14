import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: 'Skoleoverblikket',
  clientId: 'skoleoverblikket-web',
})

// Module-level promise so init() is only ever called once,
// even if AuthProvider mounts twice (React 18 Strict Mode).
const SIGNUP_TOKEN_KEY = 'skoleoverblikket_signup_token'

/** Call before navigating to /setup after signup to seed the token so init() skips the login redirect. */
export function seedPostSignupToken(accessToken: string, refreshToken: string) {
  sessionStorage.setItem(SIGNUP_TOKEN_KEY, JSON.stringify({ accessToken, refreshToken }))
}

// Module-level promise so init() is only ever called once,
// even if AuthProvider mounts twice (React 18 Strict Mode).
let initPromise: Promise<boolean> | null = null

export function getInitPromise(): Promise<boolean> {
  if (!initPromise) {
    const raw = sessionStorage.getItem(SIGNUP_TOKEN_KEY)
    const seeded = raw ? JSON.parse(raw) as { accessToken: string; refreshToken: string } : null
    if (seeded) sessionStorage.removeItem(SIGNUP_TOKEN_KEY)

    initPromise = keycloak.init({
      onLoad: seeded ? undefined : 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false,
      ...(seeded ? { token: seeded.accessToken, refreshToken: seeded.refreshToken } : {}),
    })
  }
  return initPromise
}

;(window as unknown as Record<string, unknown>).__keycloak = keycloak

export default keycloak
