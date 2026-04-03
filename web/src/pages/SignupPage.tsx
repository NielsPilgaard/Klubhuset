import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'

interface ValidationErrors {
  name?: string
  contactEmail?: string
  general?: string
}

export default function SignupPage() {
  const navigate = useNavigate()
  const timeoutIdRef = useRef<number | undefined>(undefined)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [pending, setPending] = useState(false)
  const [errors, setErrors] = useState<ValidationErrors>({})

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (timeoutIdRef.current) {
        clearTimeout(timeoutIdRef.current)
      }
    }
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErrors({})
    setPending(true)

    try {
      const res = await fetch('/api/v1/tenants', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, contactEmail: email || null }),
      })

      if (res.ok) {
        setSubmitted(true)
        // Redirect to setup wizard after a short delay so the success message is visible
        if (timeoutIdRef.current) clearTimeout(timeoutIdRef.current)
        timeoutIdRef.current = setTimeout(() => navigate('/setup'), 1500)
        return
      }

      if (res.status === 400) {
        const body = await res.json()
        const fieldErrors: ValidationErrors = {}
        for (const [field, msgs] of Object.entries(body.errors ?? {})) {
          const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof ValidationErrors
          fieldErrors[key] = (msgs as string[])[0]
        }
        setErrors(fieldErrors)
      } else {
        setErrors({ general: 'Der opstod en fejl. Prøv igen.' })
      }
    } catch {
      setErrors({ general: 'Kunne ikke oprette forbindelse til serveren.' })
    } finally {
      setPending(false)
    }
  }

  if (submitted) {
    return (
      <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-lg p-8 max-w-md w-full text-center space-y-4">
          <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-green-600">
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Skolen er oprettet!</h1>
          <p className="text-gray-500 text-sm">
            Din skole <strong>{name}</strong> er klar. En administrator kan nu logge ind og begynde at oprette skemaer.
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
        {/* Header */}
        <div className="px-8 pt-8 pb-6 border-b border-gray-100 text-center">
          <span className="font-display text-2xl font-semibold text-brand-800">Skoleplanen</span>
          <h1 className="mt-3 text-lg font-semibold text-gray-900">Opret din skole</h1>
          <p className="mt-1 text-sm text-gray-500">14 dages gratis prøveperiode · Intet kreditkort</p>
        </div>

        <form onSubmit={handleSubmit} className="px-8 py-6 space-y-5">
          {/* School name */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Skolens navn *
            </label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Vores Friskole"
              required
              data-testid="signup-name"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
            {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name}</p>}
          </div>

          {/* Contact email */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Kontakt-e-mail <span className="text-gray-400 font-normal">(valgfrit)</span>
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="rektor@vores-friskole.dk"
              data-testid="signup-email"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
            {errors.contactEmail && <p className="mt-1 text-xs text-red-600">{errors.contactEmail}</p>}
          </div>

          {errors.general && (
            <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">{errors.general}</p>
          )}

          <button
            type="submit"
            disabled={!name.trim() || !slug.trim() || pending}
            className="w-full py-2.5 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {pending ? 'Opretter skole...' : 'Opret skole'}
          </button>
        </form>

        <div className="px-8 pb-6 text-center">
          <p className="text-xs text-gray-400">
            Har du allerede en konto?{' '}
            <a href="/" className="text-brand-600 hover:underline">Log ind</a>
          </p>
        </div>
      </div>
    </div>
  )
}
