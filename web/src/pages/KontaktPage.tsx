import Logo from '../components/Logo'
import Footer from '../components/Footer'
import { usePageTitle } from '../hooks/usePageTitle'

export default function KontaktPage() {
  usePageTitle('Kontakt')
  return (
    <div className="min-h-screen bg-white font-sans text-gray-900 flex flex-col">
      <PublicNav />

      <main className="flex-1 py-20 px-6">
        <div className="max-w-xl mx-auto text-center">
          <h1 className="font-display text-4xl sm:text-5xl font-semibold text-brand-900 leading-tight mb-6">
            Kontakt
          </h1>
          <p className="text-lg text-gray-600 mb-10">
            Er du nysgerrig på Skoleoverblikket, har du spørgsmål eller feedback? Du er meget
            velkommen til at skrive — vi hører gerne fra dig.
          </p>

          <a
            href="mailto:kontakt@skoleoverblikket.dk"
            className="inline-block px-8 py-4 bg-brand-600 text-white text-base font-medium rounded-xl hover:bg-brand-700 transition-colors shadow-sm"
          >
            kontakt@skoleoverblikket.dk
          </a>

          <p className="mt-6 text-sm text-gray-400">Vi svarer typisk inden for 1–2 hverdage</p>
        </div>
      </main>

      <Footer />
    </div>
  )
}

function PublicNav() {
  return (
    <nav className="sticky top-0 z-50 bg-white border-b border-gray-100 shadow-sm">
      <div className="max-w-6xl mx-auto px-6 h-16 flex items-center justify-between">
        <a href="/" className="flex items-center gap-2">
          <Logo variant="light" size={28} />
          <span className="font-display text-xl font-semibold text-brand-800">
            Skoleoverblikket
          </span>
        </a>
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
  )
}
