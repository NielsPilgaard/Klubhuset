import { Link } from 'react-router-dom'
import Logo from './Logo'

export default function Footer() {
  return (
    <footer className="py-8 px-6 bg-brand-900 text-brand-300 text-sm">
      <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-6">
        <div className="flex items-center gap-2">
          <Logo variant="dark" size={22} />
          <span className="font-display text-white font-semibold">Skoleplanen</span>
        </div>

        <nav className="flex flex-wrap justify-center gap-x-6 gap-y-2">
          <Link
            to="/om"
            data-testid="footer-link-om"
            className="hover:text-white transition-colors"
          >
            Om Skoleplanen
          </Link>
          <Link
            to="/privatlivspolitik"
            data-testid="footer-link-privatlivspolitik"
            className="hover:text-white transition-colors"
          >
            Privatlivspolitik
          </Link>
          <Link
            to="/kontakt"
            data-testid="footer-link-kontakt"
            className="hover:text-white transition-colors"
          >
            Kontakt
          </Link>
        </nav>

        <div className="flex flex-col sm:flex-row items-center gap-4">
          <span>© {new Date().getFullYear()} Skoleplanen · Data opbevares i EU</span>
          <a href="/login" className="hover:text-white transition-colors">Log ind</a>
        </div>
      </div>
    </footer>
  )
}
