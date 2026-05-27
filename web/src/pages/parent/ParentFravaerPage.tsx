import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { usePageTitle } from '../../hooks/usePageTitle'
import { getApiV1ParentsMeOptions, getApiV1AbsenceMineOptions, postApiV1AbsenceMutation, deleteApiV1AbsenceByIdMutation } from '../../api/generated/@tanstack/react-query.gen'
import type {
  AbsenceControllerAbsenceReportDto as AbsenceReportDto,
  ParentMeControllerParentStudentDto as ParentStudentDto,
  ParentMeControllerParentMeDto as ParentMeResponse,
  AbsenceStatus,
} from '../../api/generated/types.gen'

function StatusBadge({ status }: { status: AbsenceStatus | undefined }) {
  const map: Record<AbsenceStatus, { label: string; className: string }> = {
    Reported: { label: 'Indmeldt', className: 'bg-yellow-100 text-yellow-800' },
    Confirmed: { label: 'Bekræftet', className: 'bg-green-100 text-green-800' },
    Dismissed: { label: 'Afvist', className: 'bg-red-100 text-red-800' },
  }
  const { label, className } = map[status ?? 'Reported']
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${className}`}>
      {label}
    </span>
  )
}

export default function ParentFravaerPage() {
  usePageTitle('Fravær')
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [studentId, setStudentId] = useState('')
  const [date, setDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reason, setReason] = useState('')

  const { data: meData } = useQuery({
    ...getApiV1ParentsMeOptions(),
    select: (data) => data as ParentMeResponse,
  })

  const { data: reports = [] } = useQuery({
    ...getApiV1AbsenceMineOptions(),
    select: (data) => data as AbsenceReportDto[],
  })

  const absenceMineQueryKey = [{ _id: 'getApiV1AbsenceMine' }] as const

  const reportMutation = useMutation({
    ...postApiV1AbsenceMutation(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: absenceMineQueryKey })
      setShowForm(false)
      setStudentId('')
      setDate('')
      setEndDate('')
      setReason('')
    },
  })

  const deleteMutation = useMutation({
    ...deleteApiV1AbsenceByIdMutation(),
    onSuccess: () => qc.invalidateQueries({ queryKey: absenceMineQueryKey }),
  })

  const children: ParentStudentDto[] = meData?.students ?? []

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!studentId || !date) {
      return
    }
    reportMutation.mutate({
      body: {
        studentId,
        date,
        endDate: endDate || null,
        reason: reason || null,
      },
    })
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="font-display text-2xl font-semibold text-gray-900">Fravær</h1>
        <button
          onClick={() => setShowForm(v => !v)}
          className="px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          Indmeld fravær
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white border border-gray-200 rounded-xl p-5 mb-6 space-y-4">
          <h2 className="font-semibold text-gray-900 text-sm">Nyt fravær</h2>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Barn</label>
            <select
              value={studentId}
              onChange={e => setStudentId(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              <option value="">Vælg barn</option>
              {children.map(s => (
                <option key={s.studentId} value={s.studentId}>{s.studentName}</option>
              ))}
            </select>
          </div>
          <div className="flex gap-3">
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-1">Fra dato</label>
              <input
                type="date"
                value={date}
                onChange={e => setDate(e.target.value)}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-1">Til dato (valgfrit)</label>
              <input
                type="date"
                value={endDate}
                onChange={e => setEndDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Årsag (valgfrit)</label>
            <textarea
              value={reason}
              onChange={e => setReason(e.target.value)}
              rows={2}
              placeholder="F.eks. syg, ferie..."
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
            />
          </div>
          <div className="flex gap-3">
            <button
              type="submit"
              disabled={reportMutation.isPending}
              className="px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors disabled:opacity-50"
            >
              {reportMutation.isPending ? 'Sender…' : 'Indmeld'}
            </button>
            <button
              type="button"
              onClick={() => setShowForm(false)}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
            >
              Annuller
            </button>
          </div>
          {reportMutation.isError && (
            <p className="text-sm text-red-600">Der opstod en fejl. Prøv igen.</p>
          )}
        </form>
      )}

      {reports.length === 0 && (
        <p className="text-sm text-gray-500 py-8">Ingen indmeldte fravær.</p>
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
                  <button
                    onClick={() => r.id && deleteMutation.mutate({ path: { id: r.id } })}
                    className="text-xs text-gray-400 hover:text-red-600 transition-colors"
                  >
                    Annuller
                  </button>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
