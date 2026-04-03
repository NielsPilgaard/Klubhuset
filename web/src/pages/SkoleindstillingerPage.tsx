import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, ApiError } from '../api/client'

interface SchoolSettingsDto {
  name: string
  contactEmail: string | null
  contactPhone: string | null
  logoUrl: string | null
}

export default function SkoleindstillingerPage() {
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)

  const { data, isLoading } = useQuery<SchoolSettingsDto>({
    queryKey: ['school-settings'],
    queryFn: () => api.get('/schools/settings'),
  })

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [initialized, setInitialized] = useState(false)

  // Pre-fill form once data arrives
  if (data && !initialized) {
    setName(data.name)
    setEmail(data.contactEmail ?? '')
    setPhone(data.contactPhone ?? '')
    setInitialized(true)
  }

  const [saveError, setSaveError] = useState<string | null>(null)
  const [logoError, setLogoError] = useState<string | null>(null)

  const saveMutation = useMutation({
    mutationFn: () =>
      api.put('/schools/settings', {
        name,
        contactEmail: email || null,
        contactPhone: phone || null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['school-settings'] })
      setSaveError(null)
    },
    onError: (err) => {
      setSaveError(err instanceof ApiError ? err.message : 'Der opstod en fejl.')
    },
  })

  const logoMutation = useMutation({
    mutationFn: async (file: File) => {
      const token = await getToken()
      const form = new FormData()
      form.append('file', file)
      const res = await fetch('/api/v1/schools/logo', {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: form,
      })
      if (!res.ok) {
        const text = await res.text().catch(() => res.statusText)
        throw new ApiError(res.status, text)
      }
      return res.json() as Promise<SchoolSettingsDto>
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['school-settings'] })
      setLogoError(null)
      if (fileRef.current) fileRef.current.value = ''
    },
    onError: (err) => {
      setLogoError(err instanceof ApiError ? err.message : 'Logoet kunne ikke uploades.')
    },
  })

  if (isLoading) {
    return (
      <div className="p-6 lg:p-8 max-w-2xl mx-auto space-y-6">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-40 bg-gray-100 rounded-xl animate-pulse" />
      </div>
    )
  }

  return (
    <div className="p-6 lg:p-8 max-w-2xl mx-auto space-y-8">
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">Skoleindstillinger</h1>
        <p className="mt-1 text-sm text-gray-500">Navn, kontaktoplysninger og logo</p>
      </div>

      {/* General settings */}
      <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
        <div className="px-6 py-5">
          <h2 className="text-sm font-semibold text-gray-700">Generelt</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Skolens navn *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Kontakt-e-mail</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="kontakt@skolen.dk"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Kontakttelefon</label>
            <input
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="+45 12 34 56 78"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          {saveError && <p className="text-sm text-red-600">{saveError}</p>}
        </div>
        <div className="px-6 py-4 flex justify-end">
          <button
            onClick={() => saveMutation.mutate()}
            disabled={!name.trim() || saveMutation.isPending}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {saveMutation.isPending ? 'Gemmer...' : 'Gem ændringer'}
          </button>
        </div>
      </div>

      {/* Logo upload */}
      <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
        <div className="px-6 py-5">
          <h2 className="text-sm font-semibold text-gray-700">Skolelogo</h2>
        </div>
        <div className="px-6 py-5 space-y-4">
          {data?.logoUrl && (
            <img
              src={data.logoUrl}
              alt="Skolelogo"
              className="h-16 w-auto object-contain rounded border border-gray-100"
            />
          )}
          <p className="text-sm text-gray-500">PNG, JPG eller WebP · maks. 2 MB</p>
          <input
            ref={fileRef}
            type="file"
            accept=".png,.jpg,.jpeg,.webp"
            data-testid="logo-upload"
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) logoMutation.mutate(file)
            }}
            className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100"
          />
          {logoMutation.isPending && (
            <p className="text-sm text-gray-500">Uploader...</p>
          )}
          {logoError && <p className="text-sm text-red-600">{logoError}</p>}
        </div>
      </div>
    </div>
  )
}

// Helper: get current Keycloak token without importing keycloak directly
async function getToken(): Promise<string | undefined> {
  const { default: keycloak } = await import('../auth/keycloak')
  await keycloak.updateToken(30).catch(() => keycloak.login())
  return keycloak.token
}
