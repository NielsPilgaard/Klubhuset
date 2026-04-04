import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api, ApiError } from '../api/client'
import { TimeInput } from '../components/TimeInput'
import { usePageTitle } from '../hooks/usePageTitle'

interface SchoolSettingsDto {
  name: string
  contactEmail: string | null
  contactPhone: string | null
  logoUrl: string | null
}

interface TimeSlotTemplateDto {
  id: string
  lessonDurationMinutes: number
  dayStartTime: string
  dayEndTime: string
  activeDays: string
  breaks: { id: string; startTime: string; durationMinutes: number }[]
}

interface BreakEntry {
  startTime: string
  durationMinutes: number
}

export default function SkoleindstillingerPage() {
  usePageTitle('Indstillinger')
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
  useEffect(() => {
    if (data && !initialized) {
      setName(data.name)
      setEmail(data.contactEmail ?? '')
      setPhone(data.contactPhone ?? '')
      setInitialized(true)
    }
  }, [data, initialized])

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
      setInitialized(false)
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
      // Handle empty or non-JSON responses (e.g., 204 No Content)
      if (res.status === 204 || !res.headers.get('content-length') || res.headers.get('content-type') === null) {
        return undefined
      }
      return res.json() as Promise<SchoolSettingsDto | undefined>
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
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); if (name.trim()) saveMutation.mutate() } }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Kontakt-e-mail</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); if (name.trim()) saveMutation.mutate() } }}
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
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); if (name.trim()) saveMutation.mutate() } }}
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

      {/* Skoledag */}
      <SkoledagCard />
    </div>
  )
}

function SkoledagCard() {
  const qc = useQueryClient()

  const { data: template } = useQuery<TimeSlotTemplateDto | null>({
    queryKey: ['time-slot-template'],
    queryFn: () =>
      api.get<TimeSlotTemplateDto>('/time-slot-template').catch((err) => {
        if (err?.status === 404 || err?.response?.status === 404) return null
        throw err
      }),
  })

  const [lessonDuration, setLessonDuration] = useState(45)
  const [dayStart, setDayStart] = useState('08:00')
  const [dayEnd, setDayEnd] = useState('15:00')
  const [breaks, setBreaks] = useState<BreakEntry[]>([])
  const [initialized, setInitialized] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  function validateSkoledagForm(): string | null {
    if (dayStart >= dayEnd) return 'Skoledagen skal slutte efter den starter.'
    if (lessonDuration <= 0) return 'Lektionslængde skal være større end 0.'
    return null
  }

  useEffect(() => {
    if (template !== undefined && !initialized) {
      if (template) {
        setLessonDuration(template.lessonDurationMinutes)
        setDayStart(template.dayStartTime.slice(0, 5))
        setDayEnd(template.dayEndTime.slice(0, 5))
        setBreaks(template.breaks.map(b => ({
          startTime: b.startTime.slice(0, 5),
          durationMinutes: b.durationMinutes,
        })))
      }
      setInitialized(true)
    }
  }, [template, initialized])

  const saveMutation = useMutation({
    mutationFn: () =>
      api.put('/time-slot-template', {
        lessonDurationMinutes: lessonDuration,
        dayStartTime: dayStart + ':00',
        dayEndTime: dayEnd + ':00',
        activeDays: '1,2,3,4,5',
        breaks: breaks.map(b => ({
          startTime: b.startTime + ':00',
          durationMinutes: b.durationMinutes,
        })),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['time-slot-template'] })
      setSaveError(null)
    },
    onError: () => {
      setSaveError('Kunne ikke gemme skoledag. Prøv igen.')
    },
  })

  function addBreak() {
    setBreaks(prev => [...prev, { startTime: '10:00', durationMinutes: 15 }])
  }
  function updateBreak(i: number, field: keyof BreakEntry, value: string | number) {
    setBreaks(prev => prev.map((b, idx) => idx === i ? { ...b, [field]: value } : b))
  }
  function removeBreak(i: number) {
    setBreaks(prev => prev.filter((_, idx) => idx !== i))
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 divide-y divide-gray-100">
      <div className="px-6 py-5">
        <h2 className="text-sm font-semibold text-gray-700">Skoledag</h2>
        <p className="mt-0.5 text-xs text-gray-400">Lektionslængde og pauser for en normal skoledag</p>
      </div>
      <div className="px-6 py-5 space-y-5">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Skoledag starter</label>
            <TimeInput value={dayStart} onChange={setDayStart} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Skoledag slutter</label>
            <TimeInput value={dayEnd} onChange={setDayEnd} />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Lektionslængde — {lessonDuration} minutter
          </label>
          <input
            type="range"
            min={20}
            max={90}
            step={5}
            value={lessonDuration}
            onChange={(e) => setLessonDuration(Number(e.target.value))}
            className="w-full accent-brand-600"
          />
          <div className="flex justify-between text-xs text-gray-400 mt-0.5">
            <span>20 min</span>
            <span>90 min</span>
          </div>
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="text-sm font-medium text-gray-700">Pauser</label>
            <button
              onClick={addBreak}
              className="flex items-center gap-1 text-sm text-brand-600 hover:text-brand-700"
            >
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
              </svg>
              Tilføj pause
            </button>
          </div>
          {breaks.length === 0 && (
            <p className="text-sm text-gray-400 italic">Ingen faste pauser.</p>
          )}
          <div className="space-y-2">
            {breaks.map((b, i) => (
              <div key={i} className="flex items-end gap-2">
                <div className="flex-1 grid grid-cols-2 gap-2">
                  <div>
                    <label className="block text-xs text-gray-500 mb-0.5">Starttidspunkt</label>
                    <TimeInput value={b.startTime} onChange={(v) => updateBreak(i, 'startTime', v)} />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-500 mb-0.5">Varighed (min)</label>
                    <input
                      type="number"
                      min={5}
                      max={60}
                      value={b.durationMinutes}
                      onFocus={(e) => e.target.select()}
                      onChange={(e) => updateBreak(i, 'durationMinutes', Number(e.target.value))}
                      className="w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                    />
                  </div>
                </div>
                <button
                  onClick={() => removeBreak(i)}
                  className="p-1.5 text-gray-400 hover:text-red-500 rounded-md hover:bg-red-50 transition-colors mb-0.5"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                </button>
              </div>
            ))}
          </div>
        </div>

        {saveError && <p className="text-sm text-red-600">{saveError}</p>}
      </div>
      <div className="px-6 py-4 flex justify-end">
        <button
          onClick={() => {
            const err = validateSkoledagForm()
            if (err) { setSaveError(err); return }
            saveMutation.mutate()
          }}
          disabled={saveMutation.isPending}
          className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {saveMutation.isPending ? 'Gemmer...' : 'Gem ændringer'}
        </button>
      </div>
    </div>
  )
}

// Helper: get current Keycloak token without importing keycloak directly
async function getToken(): Promise<string | undefined> {
  const { default: keycloak } = await import('../auth/keycloak')
  try {
    await keycloak.updateToken(30)
  } catch {
    keycloak.login()
    return undefined
  }
  return keycloak.token
}
