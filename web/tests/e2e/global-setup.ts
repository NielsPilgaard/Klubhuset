import { request } from '@playwright/test'

const KEYCLOAK_BASE = process.env.KEYCLOAK_URL ?? 'http://localhost:8080'
const ADMIN_EMAIL = 'admin@debugskolen.dk'

export default async function globalSetup() {
  // Clear Keycloak brute-force lockout for the shared admin account before each test session.
  // Previous runs may have locked the account via wrong-password tests running concurrently.
  try {
    const ctx = await request.newContext()

    const tokenRes = await ctx.post(
      `${KEYCLOAK_BASE}/realms/master/protocol/openid-connect/token`,
      {
        form: {
          client_id: 'admin-cli',
          username: 'admin',
          password: process.env.KEYCLOAK_ADMIN_PASSWORD ?? 'your-dev-password',
          grant_type: 'password',
        },
      },
    )
    if (!tokenRes.ok()) {
      console.warn('[global-setup] Could not get Keycloak admin token — skipping brute-force reset')
      await ctx.dispose()
      return
    }
    const { access_token } = await tokenRes.json() as { access_token: string }

    const usersRes = await ctx.get(
      `${KEYCLOAK_BASE}/admin/realms/Skoleoverblikket/users?email=${encodeURIComponent(ADMIN_EMAIL)}`,
      { headers: { Authorization: `Bearer ${access_token}` } },
    )
    if (!usersRes.ok()) {
      await ctx.dispose()
      return
    }
    const users = await usersRes.json() as { id: string }[]
    if (users.length === 0) {
      await ctx.dispose()
      return
    }

    await ctx.delete(
      `${KEYCLOAK_BASE}/admin/realms/Skoleoverblikket/attack-detection/brute-force/users/${users[0].id}`,
      { headers: { Authorization: `Bearer ${access_token}` } },
    )
    console.log('[global-setup] Keycloak brute-force reset for admin@debugskolen.dk')
    await ctx.dispose()
  } catch (e) {
    console.warn('[global-setup] Brute-force reset failed (non-fatal):', e)
  }
}
