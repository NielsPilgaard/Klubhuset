import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'

interface InvitationPreview {
  staffName: string
  email: string
  schoolName: string
  expiresAt: string
}

type PageState = 'loading' | 'invalid' | 'ready' | 'success' | 'error'

export default function InvitationAcceptPage() {
  const { token } = useParams<{ token: string }>()
  const [state, setState] = useState<PageState>('loading')
  const [preview, setPreview] = useState<InvitationPreview | null>(null)
  const [pending, setPending] = useState(false)
  const [errorMsg, setErrorMsg] = useState('')

  useEffect(() => {
    if (!token) { setState('invalid'); return }
    
    const controller = new AbortController()
    
    fetch(`/api/v1/staff-invitations/preview?token=${encodeURIComponent(token)}`, {
      signal: controller.signal
    })
      .then(async (res) => {
        if (res.ok) {
          setPreview(await res.json())
          setState('ready')
        } else {
          setState('invalid')
        }
      })
      .catch((err) => {
        if (err.name === 'AbortError') return
        setState('invalid')
      })
    
    return () => controller.abort()
  }, [token])

  async function handleAccept() {
    if (!token) return
    setPending(true)
    setErrorMsg('')
    try {
      // The Keycloak subject is provided after the user logs in via Keycloak.
      // For the invitation flow we mark the invite as accepted once the user
      // has created their account. Here we pass a placeholder that will be
      // overwritten when the user first logs in and the backend syncs their subject.
      const res = await fetch('/api/v1/staff-invitations/accept', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token, keycloakSubject: '' }),
      })
      if (res.ok || res.status === 204) {
        setState('success')
      } else {
        const body = await res.json().catch(() => ({}))
        setErrorMsg(body?.detail ?? 'Der opstod en fejl. Invitationen er muligvis allerede brugt eller udløbet.')
      }
    } catch {
      setErrorMsg('Kunne ikke oprette forbindelse til serveren.')
    } finally {
      setPending(false)
    }
  }

  if (state === 'loading') {
    return (
      <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-lg p-8 max-w-md w-full text-center" role="status" aria-live="polite" aria-busy="true">
          <div className="w-8 h-8 border-2 border-brand-600 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="mt-4 text-sm text-gray-500">Indlæser invitation…</p>
        </div>
      </div>
    )
  }

  if (state === 'invalid') {
    return (
      <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-lg p-8 max-w-md w-full text-center space-y-4">
          <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-red-600">
              <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </div>
          <h1 className="font-display text-xl font-semibold text-gray-900">Invitation ugyldig eller udløbet</h1>
          <p className="text-sm text-gray-500">Dette invitationslink er ugyldigt eller er udløbet. Bed din administrator om at sende en ny invitation.</p>
        </div>
      </div>
    )
  }

  if (state === 'success') {
    return (
      <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-lg p-8 max-w-md w-full text-center space-y-4">
          <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-green-600">
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </div>
          <h1 className="font-display text-xl font-semibold text-gray-900">Invitation accepteret!</h1>
          <p className="text-sm text-gray-500">
            Din konto er klar. Du kan nu logge ind på <strong>{preview?.schoolName}</strong> med din e-mail{' '}
            <strong>{preview?.email}</strong>.
          </p>
          <a
            href="/"
            className="inline-block mt-2 px-5 py-2.5 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
          >
            Gå til login
          </a>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-lg w-full max-w-md">
        <div className="px-8 pt-8 pb-6 border-b border-gray-100 text-center">
          <span className="font-display text-2xl font-semibold text-brand-800">Skoleplanen</span>
          <h1 className="mt-3 text-lg font-semibold text-gray-900">Du er inviteret!</h1>
          <p className="mt-1 text-sm text-gray-500">
            {preview?.schoolName} har inviteret dig til at oprette en konto
          </p>
        </div>

        <div className="px-8 py-6 space-y-4">
          <div className="bg-brand-50 rounded-xl p-4 space-y-2">
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-500 w-20 shrink-0">Navn</span>
              <span className="font-medium text-gray-900">{preview?.staffName}</span>
            </div>
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-500 w-20 shrink-0">E-mail</span>
              <span className="font-medium text-gray-900">{preview?.email}</span>
            </div>
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-500 w-20 shrink-0">Skole</span>
              <span className="font-medium text-gray-900">{preview?.schoolName}</span>
            </div>
          </div>

          <p className="text-sm text-gray-500">
            Klik på knappen herunder for at bekræfte din invitation. Du vil herefter modtage en e-mail med login-oplysninger.
          </p>

          {errorMsg && (
            <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">{errorMsg}</p>
          )}

          <button
            onClick={handleAccept}
            disabled={pending}
            className="w-full py-2.5 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {pending ? 'Bekræfter…' : 'Acceptér invitation'}
          </button>
        </div>
      </div>
    </div>
  )
}
