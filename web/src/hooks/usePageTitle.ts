import { useEffect } from 'react'

export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = title ? `${title} — Skoleplanen` : 'Skoleplanen'
    return () => {
      document.title = 'Skoleplanen'
    }
  }, [title])
}
