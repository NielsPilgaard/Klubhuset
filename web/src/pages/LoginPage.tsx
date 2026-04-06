import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import keycloak from '../auth/keycloak'

export default function LoginPage() {
  const navigate = useNavigate()

  useEffect(() => {
    if (keycloak.authenticated) {
      navigate('/dashboard', { replace: true })
    } else {
      keycloak.login({ redirectUri: window.location.origin + '/dashboard' })
    }
  }, [navigate])

  return null
}
