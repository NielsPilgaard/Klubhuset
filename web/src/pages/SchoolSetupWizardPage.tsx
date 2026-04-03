import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { StaffRole } from '../api/client'

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface WizardStep {
  id: number
  title: string
  description: string
}

const STEPS: WizardStep[] = [
  { id: 1, title: 'Skolenavn', description: 'Bekræft eller opdater skolens navn' },
  { id: 2, title: 'Logo', description: 'Upload et logo til skolen' },
  { id: 3, title: 'Lektionsstruktur', description: 'Definér varighed og pauser for en normal skoledag' },
  { id: 4, title: 'Klasser', description: 'Opret dine første klasser, f.eks. 0.a, 1.a' },
  { id: 5, title: 'Fag', description: 'Tilføj fag, f.eks. dansk, matematik' },
  { id: 6, title: 'Lokaler', description: 'Tilføj lokaler, f.eks. Lokale 1' },
  { id: 7, title: 'Medarbejdere', description: 'Invitér lærere og pædagoger' },
  { id: 8, title: 'Færdig', description: 'Din skole er klar til brug' },
]

// ---------------------------------------------------------------------------
// Step components
// ---------------------------------------------------------------------------

function StepSchoolName({ onNext, onSkip }: { onNext: () => void; onSkip: () => void }) {
  const [name, setName] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function save() {
    if (!name.trim()) { onNext(); return }
    setSaving(true)
    setError('')
    try {
      await api.put('/schools/settings', { name })
      onNext()
    } catch {
      setError('Kunne ikke gemme skolenavn. Prøv igen.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-600">
        Hvad hedder din skole? Det vises på skemaer og i invitationsemails.
      </p>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Skolens navn</label>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Vores Friskole"
          autoFocus
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
        />
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3 pt-2">
        <button
          onClick={save}
          disabled={saving}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Gemmer…' : 'Gem og fortsæt'}
        </button>
        <button onClick={onSkip} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">
          Spring over
        </button>
      </div>
    </div>
  )
}

function StepLogo({ onNext, onSkip }: { onNext: () => void; onSkip: () => void }) {
  const [file, setFile] = useState<File | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function upload() {
    if (!file) { onNext(); return }
    setSaving(true)
    setError('')
    try {
      const form = new FormData()
      form.append('file', file)

      const token = await getToken()
      const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {}

      const res = await fetch('/api/v1/schools/logo', {
        method: 'POST',
        body: form,
        headers,
      })
      if (res.ok) { onNext() }
      else { setError('Kunne ikke uploade logo. Prøv igen fra indstillinger.') }
    } catch {
      setError('Uploaden fejlede. Du kan uploade logo fra Indstillinger.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-600">
        Upload et logo til din skole. Det vises på udskrevne skemaer. Max 2 MB, PNG eller JPG.
      </p>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Logo</label>
        <input
          type="file"
          accept="image/png,image/jpeg,image/webp"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          className="block w-full text-sm text-gray-500 file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100"
        />
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3 pt-2">
        <button
          onClick={upload}
          disabled={saving || !file}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Uploader…' : 'Upload og fortsæt'}
        </button>
        <button onClick={onSkip} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">
          Spring over
        </button>
      </div>
    </div>
  )
}

interface BreakEntry {
  startTime: string
  durationMinutes: number
}

function StepTimeSlots({ onNext, onSkip }: { onNext: () => void; onSkip: () => void }) {
  const [lessonDuration, setLessonDuration] = useState(45)
  const [dayStart, setDayStart] = useState('08:00')
  const [dayEnd, setDayEnd] = useState('15:00')
  const [breaks, setBreaks] = useState<BreakEntry[]>([])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function addBreak() {
    setBreaks((prev) => [...prev, { startTime: '10:00', durationMinutes: 15 }])
  }

  function updateBreak(i: number, field: keyof BreakEntry, value: string | number) {
    setBreaks((prev) => prev.map((b, idx) => idx === i ? { ...b, [field]: value } : b))
  }

  function removeBreak(i: number) {
    setBreaks((prev) => prev.filter((_, idx) => idx !== i))
  }

  async function save() {
    setSaving(true)
    setError('')
    try {
      await api.put('/time-slot-template', {
        lessonDurationMinutes: lessonDuration,
        dayStartTime: dayStart + ':00',
        dayEndTime: dayEnd + ':00',
        activeDays: 'MTWTF',
        breaks: breaks.map((b) => ({
          startTime: b.startTime + ':00',
          durationMinutes: b.durationMinutes,
        })),
      })
      onNext()
    } catch {
      setError('Kunne ikke gemme lektionsstruktur. Prøv igen.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-600">
        Sæt standard lektionslængde og skoledagens rammer. Du kan altid justere det senere.
      </p>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Skoledag starter</label>
          <input
            type="time"
            value={dayStart}
            onChange={(e) => setDayStart(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Skoledag slutter</label>
          <input
            type="time"
            value={dayEnd}
            onChange={(e) => setDayEnd(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
          />
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
          <p className="text-sm text-gray-400 italic">Ingen faste pauser — tilføj om nødvendigt.</p>
        )}
        <div className="space-y-2">
          {breaks.map((b, i) => (
            <div key={i} className="flex items-center gap-2">
              <div className="flex-1 grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-xs text-gray-500 mb-0.5">Starttidspunkt</label>
                  <input
                    type="time"
                    value={b.startTime}
                    onChange={(e) => updateBreak(i, 'startTime', e.target.value)}
                    className="w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                  />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-0.5">Varighed (min)</label>
                  <input
                    type="number"
                    min={5}
                    max={60}
                    value={b.durationMinutes}
                    onChange={(e) => updateBreak(i, 'durationMinutes', Number(e.target.value))}
                    className="w-full px-2 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
                  />
                </div>
              </div>
              <button
                onClick={() => removeBreak(i)}
                className="mt-4 p-1.5 text-gray-400 hover:text-red-500 rounded-md hover:bg-red-50 transition-colors"
              >
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>
          ))}
        </div>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3 pt-2">
        <button
          onClick={save}
          disabled={saving}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Gemmer…' : 'Gem og fortsæt'}
        </button>
        <button onClick={onSkip} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">
          Spring over
        </button>
      </div>
    </div>
  )
}

function StepCreateItems({
  noun,
  plural,
  placeholder,
  apiPath,
  onNext,
  onSkip,
}: {
  noun: string
  plural: string
  placeholder: string
  apiPath: string
  onNext: () => void
  onSkip: () => void
}) {
  const [items, setItems] = useState<string[]>([''])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState<string[]>([])

  function addRow() { setItems((prev) => [...prev, '']) }
  function updateRow(i: number, val: string) {
    setItems((prev) => prev.map((v, idx) => (idx === i ? val : v)))
  }
  function removeRow(i: number) {
    setItems((prev) => prev.filter((_, idx) => idx !== i))
  }

  async function save() {
    const names = items.map((n) => n.trim()).filter(Boolean)
    if (names.length === 0) { onNext(); return }
    setSaving(true)
    setError('')
    try {
      await Promise.all(names.map((name) => api.post(apiPath, { name })))
      setSaved(names)
      onNext()
    } catch {
      setError(`Kunne ikke oprette ${plural.toLowerCase()}. Prøv igen.`)
    } finally {
      setSaving(false)
    }
  }

  if (saved.length > 0) return null

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-600">
        Tilføj de {plural.toLowerCase()}, du vil starte med. Du kan altid tilføje flere senere.
      </p>
      <div className="space-y-2">
        {items.map((val, i) => (
          <div key={i} className="flex gap-2">
            <input
              value={val}
              onChange={(e) => updateRow(i, e.target.value)}
              placeholder={placeholder}
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addRow() } }}
            />
            {items.length > 1 && (
              <button
                onClick={() => removeRow(i)}
                className="p-2 text-gray-400 hover:text-red-500 rounded-md hover:bg-red-50 transition-colors"
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            )}
          </div>
        ))}
        <button
          onClick={addRow}
          className="flex items-center gap-1.5 text-sm text-brand-600 hover:text-brand-700 mt-1"
        >
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Tilføj {noun.toLowerCase()}
        </button>
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3 pt-2">
        <button
          onClick={save}
          disabled={saving}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Opretter…' : `Opret og fortsæt`}
        </button>
        <button onClick={onSkip} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">
          Spring over
        </button>
      </div>
    </div>
  )
}

const ROLE_OPTIONS: { value: StaffRole; label: string; hint: string }[] = [
  { value: 'Teacher', label: 'Lærer', hint: 'Underviser klasser' },
  { value: 'Aide', label: 'Pædagog', hint: 'Støtter undervisningen' },
  { value: 'Substitute', label: 'Vikar', hint: 'Vikarierer ved fravær' },
]

function StepInviteStaff({ onNext, onSkip }: { onNext: () => void; onSkip: () => void }) {
  const [emails, setEmails] = useState('')
  const [role, setRole] = useState<StaffRole>('Teacher')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [results, setResults] = useState<{ email: string; ok: boolean }[]>([])

  async function invite() {
    const list = emails
      .split(/[\n,;]/)
      .map((e) => e.trim())
      .filter((e) => e.includes('@'))
    if (list.length === 0) { onNext(); return }
    setSaving(true)
    setError('')
    try {
      const outcome: { email: string; ok: boolean }[] = []
      for (const email of list) {
        try {
          const staff = await api.post<{ id: string }>('/staff', {
            name: email.split('@')[0],
            email,
            role,
          })
          await api.post(`/staff-invitations/invite/${staff.id}`, {})
          outcome.push({ email, ok: true })
        } catch {
          outcome.push({ email, ok: false })
        }
      }
      setResults(outcome)
    } catch {
      setError('Der opstod en fejl. Prøv igen.')
    } finally {
      setSaving(false)
    }
  }

  if (results.length > 0) {
    const succeeded = results.filter((r) => r.ok).length
    return (
      <div className="space-y-5">
        <div className="bg-green-50 border border-green-200 rounded-xl p-4">
          <p className="text-sm font-medium text-green-800">{succeeded} invitation(er) sendt</p>
          <ul className="mt-2 space-y-1">
            {results.map((r) => (
              <li key={r.email} className="flex items-center gap-2 text-sm">
                {r.ok
                  ? <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-green-600 shrink-0"><polyline points="20 6 9 17 4 12" /></svg>
                  : <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-red-500 shrink-0"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>
                }
                <span className={r.ok ? 'text-gray-700' : 'text-red-600'}>{r.email}</span>
              </li>
            ))}
          </ul>
        </div>
        <button
          onClick={onNext}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          Fortsæt
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-600">
        Indsæt e-mailadresser på de medarbejdere, du vil invitere. Adskil med komma, semikolon eller linjeskift.
      </p>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Rolle</label>
        <div className="grid grid-cols-3 gap-2">
          {ROLE_OPTIONS.map((r) => (
            <button
              key={r.value}
              type="button"
              onClick={() => setRole(r.value)}
              className={`px-3 py-2.5 rounded-lg border text-left transition-colors ${
                role === r.value
                  ? 'border-brand-500 bg-brand-50 ring-1 ring-brand-500'
                  : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
              }`}
            >
              <span className="block text-sm font-medium text-gray-800">{r.label}</span>
              <span className="block text-xs text-gray-500 mt-0.5">{r.hint}</span>
            </button>
          ))}
        </div>
        <p className="text-xs text-gray-400 mt-1.5">Du kan ændre rollen på den enkelte medarbejder bagefter.</p>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">E-mailadresser</label>
        <textarea
          value={emails}
          onChange={(e) => setEmails(e.target.value)}
          rows={4}
          placeholder={"anne@skole.dk\nbrian@skole.dk\nchristina@skole.dk"}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent resize-none"
        />
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex gap-3 pt-2">
        <button
          onClick={invite}
          disabled={saving}
          className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Sender…' : 'Send invitationer'}
        </button>
        <button onClick={onSkip} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">
          Spring over
        </button>
      </div>
    </div>
  )
}

function StepDone({ onFinish }: { onFinish: () => void }) {
  return (
    <div className="space-y-5 text-center py-4">
      <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="text-green-600">
          <polyline points="20 6 9 17 4 12" />
        </svg>
      </div>
      <div>
        <h3 className="text-lg font-semibold text-gray-900">Din skole er sat op!</h3>
        <p className="mt-1 text-sm text-gray-500">
          Du er klar til at begynde at bygge skemaer. Du kan altid ændre indstillingerne under Indstillinger.
        </p>
      </div>
      <button
        onClick={onFinish}
        className="px-6 py-2.5 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
      >
        Gå til oversigt
      </button>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Progress indicator
// ---------------------------------------------------------------------------

function ProgressBar({ current, total }: { current: number; total: number }) {
  const pct = Math.round(((current - 1) / (total - 1)) * 100)
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-xs text-gray-500">
        <span>Trin {current} af {total}</span>
        <span>{pct}% færdig</span>
      </div>
      <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
        <div
          className="h-full bg-brand-600 rounded-full transition-all duration-300"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main wizard
// ---------------------------------------------------------------------------

export default function SchoolSetupWizardPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState(1)

  const advance = () => setStep((s) => Math.min(s + 1, STEPS.length))
  const skip = () => setStep((s) => Math.min(s + 1, STEPS.length))

  const current = STEPS[step - 1]

  return (
    <div className="min-h-screen bg-brand-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-lg w-full max-w-lg">
        {/* Header */}
        <div className="px-8 pt-8 pb-5 border-b border-gray-100">
          <div className="flex items-center justify-between mb-4">
            <span className="font-display text-xl font-semibold text-brand-800">Skoleplanen</span>
            <button
              onClick={() => navigate('/dashboard')}
              className="text-xs text-gray-400 hover:text-gray-600"
            >
              Gem og afslut
            </button>
          </div>
          <ProgressBar current={step} total={STEPS.length} />
        </div>

        {/* Step header */}
        <div className="px-8 pt-6 pb-2">
          <h1 className="font-display text-lg font-semibold text-gray-900">{current.title}</h1>
          <p className="mt-0.5 text-sm text-gray-500">{current.description}</p>
        </div>

        {/* Step body */}
        <div className="px-8 pb-8 pt-4">
          {step === 1 && <StepSchoolName onNext={advance} onSkip={skip} />}
          {step === 2 && <StepLogo onNext={advance} onSkip={skip} />}
          {step === 3 && <StepTimeSlots onNext={advance} onSkip={skip} />}
          {step === 4 && (
            <StepCreateItems
              noun="Klasse"
              plural="Klasser"
              placeholder="f.eks. 0.a"
              apiPath="/classes"
              onNext={advance}
              onSkip={skip}
            />
          )}
          {step === 5 && (
            <StepCreateItems
              noun="Fag"
              plural="Fag"
              placeholder="f.eks. dansk"
              apiPath="/courses"
              onNext={advance}
              onSkip={skip}
            />
          )}
          {step === 6 && (
            <StepCreateItems
              noun="Lokale"
              plural="Lokaler"
              placeholder="f.eks. Lokale 1"
              apiPath="/rooms"
              onNext={advance}
              onSkip={skip}
            />
          )}
          {step === 7 && <StepInviteStaff onNext={advance} onSkip={skip} />}
          {step === 8 && <StepDone onFinish={() => navigate('/dashboard')} />}
        </div>

        {/* Step dots */}
        <div className="px-8 pb-6 flex justify-center gap-1.5">
          {STEPS.map((s) => (
            <button
              key={s.id}
              onClick={() => setStep(s.id)}
              className={`w-2 h-2 rounded-full transition-colors ${
                s.id === step ? 'bg-brand-600' : s.id < step ? 'bg-brand-300' : 'bg-gray-200'
              }`}
              aria-label={`Gå til trin ${s.id}: ${s.title}`}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

// Helper: get current Keycloak token without importing keycloak directly
async function getToken(): Promise<string | undefined> {
  try {
    const { default: keycloak } = await import('../auth/keycloak')
    await keycloak.updateToken(30).catch(() => keycloak.login())
    return keycloak.token
  } catch {
    return undefined
  }
}
