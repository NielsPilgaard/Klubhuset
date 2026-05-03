import Logo from '../components/Logo'
import Footer from '../components/Footer'
import CookieBanner from '../components/CookieBanner'
import { usePageTitle } from '../hooks/usePageTitle'

export default function LandingPage() {
  usePageTitle('')
  return (
    <div className="min-h-screen bg-white font-sans text-gray-900">

      {/* Nav */}
      <nav className="sticky top-0 z-50 bg-white border-b border-gray-100 shadow-sm">
        <div className="max-w-6xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Logo variant="light" size={28} />
            <span className="font-display text-xl font-semibold text-brand-800">Skoleplanen</span>
          </div>
          <div className="flex items-center gap-4">
            <a href="/login" className="text-sm text-gray-600 hover:text-brand-700 transition-colors">
              Log ind
            </a>
            <a
              href="/signup"
              className="text-sm px-4 py-2 bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors font-medium"
            >
              Prøv gratis
            </a>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="py-24 px-6 text-center bg-gradient-to-b from-brand-50 to-white">
        <div className="max-w-3xl mx-auto">
          <h1 className="font-display text-5xl sm:text-6xl font-semibold text-brand-900 leading-tight">
            Det enkle skema —<br />bygget til friskoler
          </h1>
          <p className="mt-6 text-lg text-gray-600 max-w-xl mx-auto">
            Byg, administrer og udskriv ugeskemaer med automatisk konfliktkontrol. Uden kompleksitet.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <a
              href="/signup"
              className="px-6 py-3 bg-brand-600 text-white text-base font-medium rounded-lg hover:bg-brand-700 transition-colors"
            >
              Start din gratis prøveperiode
            </a>
            <a
              href="#priser"
              className="px-6 py-3 bg-white text-brand-700 text-base font-medium rounded-lg border border-brand-200 hover:bg-brand-50 transition-colors"
            >
              Se priser
            </a>
          </div>
          <p className="mt-5 text-sm text-gray-400">
            Intet kreditkort · Ingen binding · Data opbevares i EU
          </p>
        </div>
      </section>

      {/* Features */}
      <section className="py-20 px-6 bg-white">
        <div className="max-w-6xl mx-auto">
          <h2 className="font-display text-3xl font-semibold text-center text-gray-900 mb-12">
            Alt hvad din skole behøver
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            <FeatureCard
              icon={
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6">
                  <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                </svg>
              }
              title="Konfliktkontrol i realtid"
              description="Systemet advarer med det samme om dobbeltbookinger."
            />
            <FeatureCard
              icon={
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6">
                  <rect x="3" y="3" width="18" height="18" rx="2" />
                  <path d="M3 9h18M9 21V9" />
                </svg>
              }
              title="Skemabygger"
              description="Træk og slip lektioner på plads. Simpelt og overskueligt — for alle."
            />
            <FeatureCard
              icon={
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
              }
              title="Medarbejderoversigt"
              description="Se alle medarbejderes skemaer samlet — lærere, pædagoger og vikarer."
            />
            <FeatureCard
              icon={
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6">
                  <polyline points="6 9 6 2 18 2 18 9" />
                  <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2" />
                  <rect x="6" y="14" width="12" height="8" />
                </svg>
              }
              title="Udskriv skemaer"
              description="Udskriv klasse-, lærer- og lokaleskemaer med ét klik. Print-venligt format."
            />
            <FeatureCard
              icon={
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                </svg>
              }
              title="Filhåndtering"
              description="Upload og del filer pr. fag. Let tilgængeligt for alle medarbejdere."
            />
          </div>
        </div>
      </section>

      {/* Pricing */}
      <section id="priser" className="py-20 px-6 bg-brand-900 text-white">
        <div className="max-w-5xl mx-auto text-center">
          <h2 className="font-display text-3xl font-semibold mb-3">Enkel og gennemsigtig pris</h2>
          <p className="text-brand-200 mb-12">Enkel pris — udvid efter behov.</p>
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start max-w-3xl mx-auto">

            {/* Basis card */}
            <div className="bg-white text-gray-900 rounded-2xl shadow-xl overflow-visible">
              <div className="px-8 pt-8 pb-6 bg-brand-50 border-b border-brand-100">
                <p className="text-sm font-medium text-brand-600 uppercase tracking-wide">Basis</p>
                <div className="mt-2 flex items-end gap-1 justify-center">
                  <span className="font-display text-5xl font-semibold text-brand-900">299</span>
                  <span className="text-lg text-gray-500 mb-2">kr/md</span>
                </div>
                <p className="text-sm text-gray-500 mt-1">inkl. moms · pr. skole</p>
              </div>
              <div className="px-8 py-6 space-y-3 text-left">
                {[
                  'Ubegrænset antal klasser og lærere',
                  'Konfliktkontrol – ingen dobbeltbookede lærere eller lokaler',
                  'Medarbejder- og lokaleskemaer',
                  'Printbare skemaer',
                  'Filhåndtering (100 GB)',
                  'E-mail support (svar inden 5 hverdage)',
                ].map((item) => (
                  <div key={item} className="flex items-center gap-3 text-sm">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="w-4 h-4 text-brand-600 shrink-0">
                      <polyline points="20 6 9 17 4 12" />
                    </svg>
                    <span>{item}</span>
                  </div>
                ))}
                <div className="flex items-center gap-3 text-sm">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="w-4 h-4 text-brand-600 shrink-0">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  <span>Ingen bindingsperiode — opsig når som helst</span>
                  <RefundTooltip />
                </div>
                <div className="pt-4">
                  <a
                    href="/signup"
                    className="block w-full text-center py-3 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
                  >
                    Start 14 dages gratis prøve
                  </a>
                  <p className="text-xs text-center text-gray-400 mt-2">Intet kreditkort påkrævet</p>
                </div>
              </div>
            </div>

            {/* Add-on modules teaser card */}
            <div className="bg-gray-50 text-gray-900 rounded-2xl border border-dashed border-gray-300 overflow-hidden text-left">
              <div className="px-8 pt-8 pb-6 border-b border-gray-200">
                <div className="flex items-center gap-2 mb-2">
                  <p className="text-sm font-medium text-gray-700 uppercase tracking-wide">Tillægsmoduler</p>
                  <span className="text-xs px-2 py-0.5 bg-gray-200 text-gray-500 rounded-full font-medium">Kommer snart</span>
                </div>
                <p className="text-sm text-gray-500 mt-1">Udvid Basis med moduler tilpasset din skole — betal kun for det, I bruger.</p>
              </div>
              <div className="px-8 py-6 space-y-4">
                <ComingSoonItem
                  title="Forældremodul"
                  description="Ugeplaner, kontaktbog, kontakter og kalenderadgang"
                />
                <ComingSoonItem
                  title="Bestyrelsesmodul"
                  description={'Dokumentdeling og "stå mål med"-assistent'}
                />
                <p className="text-xs text-gray-400 pt-2">Samles på én månedlig faktura.</p>
              </div>
            </div>

          </div>
        </div>
      </section>

      {/* Trust */}
      <section className="py-16 px-6 bg-white border-t border-gray-100">
        <div className="max-w-4xl mx-auto grid grid-cols-1 sm:grid-cols-3 gap-8 text-center">
          <TrustItem
            icon={
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6 mx-auto">
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              </svg>
            }
            title="Data opbevares i EU"
            description="Alle data gemmes på EU-baserede servere i overensstemmelse med GDPR."
          />
          <TrustItem
            icon={
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6 mx-auto">
                <line x1="12" y1="1" x2="12" y2="23" />
                <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
              </svg>
            }
            title="Gennemsigtige priser"
            description="299 kr/md for Basis. Ingen bindingsperiode. Ingen skjulte gebyrer."
          />
          <TrustItem
            icon={
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-6 h-6 mx-auto">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 16 14" />
              </svg>
            }
            title="Simpelt fra dag ét"
            description="Ingen oplæring nødvendig. Klar til brug på 10 minutter."
          />
        </div>
      </section>

      {/* CTA footer */}
      <section className="py-20 px-6 bg-brand-50 text-center">
        <div className="max-w-2xl mx-auto">
          <h2 className="font-display text-3xl font-semibold text-brand-900">
            Klar til at prøve?
          </h2>
          <p className="mt-3 text-gray-600">14 dage gratis. Intet kreditkort. Kom i gang på få minutter.</p>
          <a
            href="/signup"
            className="inline-block mt-6 px-8 py-3 bg-brand-600 text-white text-base font-medium rounded-lg hover:bg-brand-700 transition-colors"
          >
            Opret din skole gratis
          </a>
        </div>
      </section>

      <Footer />
      <CookieBanner />

    </div>
  )
}

function FeatureCard({ icon, title, description }: { icon: React.ReactNode; title: string; description: string }) {
  return (
    <div className="p-6 rounded-xl border border-gray-100 bg-brand-50 hover:border-brand-200 transition-colors">
      <div className="w-10 h-10 bg-brand-100 text-brand-700 rounded-lg flex items-center justify-center mb-4">
        {icon}
      </div>
      <h3 className="font-semibold text-gray-900 mb-1">{title}</h3>
      <p className="text-sm text-gray-600 leading-relaxed">{description}</p>
    </div>
  )
}

function TrustItem({ icon, title, description }: { icon: React.ReactNode; title: string; description: string }) {
  return (
    <div className="space-y-2">
      <div className="text-brand-600">{icon}</div>
      <h3 className="font-semibold text-gray-900">{title}</h3>
      <p className="text-sm text-gray-500">{description}</p>
    </div>
  )
}

function ComingSoonItem({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex items-start gap-3">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="w-4 h-4 text-gray-400 shrink-0 mt-0.5">
        <circle cx="12" cy="12" r="10" />
        <polyline points="12 6 12 12 16 14" />
      </svg>
      <div>
        <span className="text-sm font-medium text-gray-500">{title}</span>
        <p className="text-xs text-gray-400">{description}</p>
      </div>
    </div>
  )
}

function RefundTooltip() {
  return (
    <div className="group relative inline-flex cursor-default select-none shrink-0">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="w-3.5 h-3.5 text-gray-400 group-hover:text-gray-600 transition-colors">
        <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
        <path d="M7 11V7a5 5 0 0 1 9.9-1" />
      </svg>
      <div className="pointer-events-none absolute bottom-full right-0 mb-2 w-64 rounded-lg bg-gray-900 px-3 py-2 text-xs text-gray-100 opacity-0 group-hover:opacity-100 transition-opacity z-10 shadow-lg text-left">
        Abonnementet kan opsiges til enhver tid. Der ydes ikke refusion for den igangværende måneds betaling.
      </div>
    </div>
  )
}
