import { useEffect } from 'react'

export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = title ? `${title} — Skoleoverblikket` : 'Skoleoverblikket'
    return () => {
      document.title = 'Skoleoverblikket'
    }
  }, [title])
}
