import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { usePageTitle } from '../hooks/usePageTitle'
import { useAuth } from '../auth/useAuth'
import type { AbsenceControllerAbsenceReportDto as AbsenceReportDto } from '../api/generated/types.gen'

function StatusBadge({ status }: { status: AbsenceReportDto['status'] }) {
  const map = {
    Reported: { label: 'Indmeldt', className: 'bg-yellow-100 text-yellow-800' },
    Confirmed: { label: 'Bekræftet', className: 'bg-green-100 text-green-800' },
    Dismissed: { label: 'Afvist', className: 'bg-red-100 text-red-800' },
  }
  const { label, className } = map[status!]
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${className}`}>
      {label}
    </span>
  )
}

export default function FravaerPage() {
  usePageTitle('Fravær')
  const { token } = useAuth()
  const qc = useQueryClient()
  const today = new Date().toISOString().slice(0, 10)
  const monthStart = today.slice(0, 8) + '01'
  const [from, setFrom] = useState(monthStart)
  const [to, setTo] = useState(today)
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: reports = [], isLoading } = useQuery<AbsenceReportDto[]>({
    queryKey: ['absence', from, to],
    queryFn: async () => {
      const params = new URLSearchParams({ from, to })
      const res = await fetch(`/api/v1/absence?${params}`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      return res.json()
    },
  })

  const confirmMutation = useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/v1/absence/${id}/confirm`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      if (!res.ok) throw new Error('Kunne ikke bekræfte fravær')
    },
    onSuccess: () => { setActionError(null); qc.invalidateQueries({ queryKey: ['absence'] }) },
    onError: (err: Error) => setActionError(err.message),
  })

  const dismissMutation = useMutation({
    mutationFn: async (id: string) => {
      const res = await fetch(`/api/v1/absence/${id}/dismiss`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      if (!res.ok) throw new Error('Kunne ikke afvise fravær')
    },
    onSuccess: () => { setActionError(null); qc.invalidateQueries({ queryKey: ['absence'] }) },
    onError: (err: Error) => setActionError(err.message),
  })

  const unconfirmed = reports.filter(r => r.status === 'Reported')

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="font-display text-2xl font-semibold text-gray-900">Fravær</h1>
          {unconfirmed.length > 0 && (
            <p className="text-sm text-yellow-700 mt-1">
              {unconfirmed.length} ubekræftet{unconfirmed.length !== 1 ? 'e' : ''}
            </p>
          )}
        </div>
        <div className="flex gap-2 items-center">
          <input
            type="date"
            value={from}
            onChange={e => setFrom(e.target.value)}
            className="px-2 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
          <span className="text-gray-400 text-sm">–</span>
          <input
            type="date"
            value={to}
            onChange={e => setTo(e.target.value)}
            className="px-2 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
        </div>
      </div>

      {actionError && (
        <p className="mb-4 text-sm text-red-600">{actionError}</p>
      )}

      {isLoading && (
        <div className="flex justify-center py-16">
          <div className="w-6 h-6 border-2 border-brand-600 border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {!isLoading && reports.length === 0 && (
        <p className="text-sm text-gray-500 py-8">Ingen fravær i perioden.</p>
      )}

      <div className="space-y-3">
        {reports.map(r => (
          <div key={r.id} className="bg-white border border-gray-200 rounded-xl p-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="font-medium text-gray-900 text-sm">{r.studentName}</p>
                <p className="text-sm text-gray-600 mt-0.5">
                  {r.date}{r.endDate ? ` – ${r.endDate}` : ''}
                  {r.reason ? ` · ${r.reason}` : ''}
                </p>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <StatusBadge status={r.status} />
                {r.status === 'Reported' && (
                  <>
                    <button
                      onClick={() => confirmMutation.mutate(r.id!)}
                      disabled={confirmMutation.isPending}
                      className="px-2.5 py-1 text-xs font-medium bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors disabled:opacity-50"
                    >
                      Bekræft
                    </button>
                    <button
                      onClick={() => dismissMutation.mutate(r.id!)}
                      disabled={dismissMutation.isPending}
                      className="px-2.5 py-1 text-xs font-medium border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors disabled:opacity-50"
                    >
                      Afvis
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
