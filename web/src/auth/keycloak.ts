import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: 'Skoleplanen',
  clientId: 'skoleplanen-web',
})

// Module-level promise so init() is only ever called once,
// even if AuthProvider mounts twice (React 18 Strict Mode).
let initPromise: Promise<boolean> | null = null

export function getInitPromise(): Promise<boolean> {
  if (!initPromise) {
    initPromise = keycloak.init({
      onLoad: 'login-required',
      pkceMethod: 'S256',
      checkLoginIframe: false,
    })
  }
  return initPromise
}

export default keycloak
