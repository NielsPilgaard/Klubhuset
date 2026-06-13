import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import Logo from '../components/Logo'

interface InvitationPreview {
  boardMemberName: string
  email: string
  schoolName: string
  expiresAt: string
}

export default function BoardInvitationPage() {
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const [preview, setPreview] = useState<InvitationPreview | null>(null)
  const [loading, setLoading] = useState(true)
  const [accepting, setAccepting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [accepted, setAccepted] = useState(false)

  useEffect(() => {
    if (!token) return
    fetch(`/api/v1/board-invitations/preview?token=${encodeURIComponent(token)}`)
      .then((res) => {
        if (!res.ok) throw new Error('Ugyldigt eller udløbet link')
        return res.json() as Promise<InvitationPreview>
      })
      .then(setPreview)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [token])

  async function handleAccept() {
    if (!token) return
    setAccepting(true)
    setError(null)
    try {
      const res = await fetch(`/api/v1/board-invitations/${encodeURIComponent(token)}/accept`, {
        method: 'POST',
      })
      if (!res.ok) throw new Error('Invitation kunne ikke accepteres')
      setAccepted(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Der opstod en fejl')
    } finally {
      setAccepting(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-sm border border-gray-200 p-8 space-y-6">
        <div className="flex justify-center">
          <Logo variant="light" size={40} />
        </div>

        {loading ? (
          <div className="animate-pulse space-y-3">
            <div className="h-5 bg-gray-200 rounded w-3/4 mx-auto" />
            <div className="h-4 bg-gray-100 rounded w-1/2 mx-auto" />
          </div>
        ) : error ? (
          <div className="text-center">
            <p className="font-semibold text-gray-900">Ugyldigt link</p>
            <p className="mt-1 text-sm text-gray-500">{error}</p>
            <button
              onClick={() => navigate('/')}
              className="mt-4 px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
            >
              Gå til forsiden
            </button>
          </div>
        ) : accepted ? (
          <div className="text-center">
            <p className="font-semibold text-gray-900">Invitation accepteret</p>
            <p className="mt-1 text-sm text-gray-500">
              Du kan nu logge ind med din e-mail og den midlertidige adgangskode du modtog.
            </p>
            <button
              onClick={() => navigate('/login')}
              className="mt-4 px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
            >
              Log ind
            </button>
          </div>
        ) : preview ? (
          <div className="text-center space-y-4">
            <div>
              <p className="font-semibold text-gray-900">Du er inviteret til bestyrelsen</p>
              <p className="mt-1 text-sm text-gray-500">
                {preview.schoolName} har inviteret {preview.boardMemberName} til bestyrelsesmodulet
                på Skoleoverblikket.
              </p>
            </div>
            <div className="bg-gray-50 rounded-xl p-4 text-left text-sm space-y-1">
              <p className="text-gray-500">
                E-mail: <span className="text-gray-900">{preview.email}</span>
              </p>
              <p className="text-gray-500">
                Skole: <span className="text-gray-900">{preview.schoolName}</span>
              </p>
            </div>
            <p className="text-xs text-gray-400">
              Linket udløber {new Date(preview.expiresAt).toLocaleDateString('da-DK')}
            </p>
            <button
              onClick={handleAccept}
              disabled={accepting}
              className="w-full px-4 py-2.5 text-sm font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {accepting ? 'Accepterer...' : 'Acceptér invitation'}
            </button>
          </div>
        ) : null}
      </div>
    </div>
  )
}
