import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { usePageTitle } from '../hooks/usePageTitle'
import { useAuth } from '../auth/useAuth'
import {
  getApiV1ClassesOptions,
  postApiV1BroadcastEmailPreviewMutation,
  postApiV1BroadcastEmailMutation,
  getApiV1BroadcastEmailLogOptions,
} from '../api/generated/@tanstack/react-query.gen'
import type {
  ClassesControllerClassDto,
  BroadcastControllerBroadcastLogDto,
} from '../api/generated/types.gen'

type Step = 'compose' | 'confirm' | 'done'

export default function BroadcastPage() {
  usePageTitle('Udsend e-mail')
  const { isAdmin } = useAuth()

  const [step, setStep] = useState<Step>('compose')
  const [classId, setClassId] = useState<string>('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [recipientCount, setRecipientCount] = useState<number | null>(null)

  const { data: classes = [] } = useQuery({
    ...getApiV1ClassesOptions(),
    select: (data) => data as ClassesControllerClassDto[],
  })

  const { data: log = [] } = useQuery({
    ...getApiV1BroadcastEmailLogOptions(),
    enabled: isAdmin,
    select: (data) => data as BroadcastControllerBroadcastLogDto[],
  })

  const previewMutation = useMutation({
    ...postApiV1BroadcastEmailPreviewMutation(),
    onSuccess: (data) => {
      setRecipientCount(data.recipientCount ?? 0)
      setStep('confirm')
    },
  })

  const sendMutation = useMutation({
    ...postApiV1BroadcastEmailMutation(),
    onSuccess: () => {
      setStep('done')
    },
  })

  function handlePreview(e: React.FormEvent) {
    e.preventDefault()
    if (!subject.trim() || !body.trim()) return
    previewMutation.mutate({
      body: { classId: classId || null },
    })
  }

  function handleSend() {
    sendMutation.mutate({
      body: {
        classId: classId || null,
        subject,
        body,
      },
    })
  }

  function handleReset() {
    setStep('compose')
    setClassId('')
    setSubject('')
    setBody('')
    setRecipientCount(null)
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8 space-y-8">
      <h1 className="font-display text-2xl font-semibold text-gray-900">Udsend e-mail</h1>

      {step === 'compose' && (
        <form
          onSubmit={handlePreview}
          className="bg-white border border-gray-200 rounded-xl p-6 space-y-5"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Modtagere</label>
            <select
              value={classId}
              onChange={(e) => setClassId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              {isAdmin && <option value="">Hele skolen</option>}
              {classes.map((c) => (
                <option key={c.id} value={c.id ?? ''}>
                  {c.name}
                </option>
              ))}
            </select>
            {!isAdmin && (
              <p className="mt-1 text-xs text-gray-500">
                Du kan kun sende til klasser du underviser.
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Emne</label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              required
              maxLength={200}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              placeholder="Emne for e-mailen"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Besked</label>
            <textarea
              value={body}
              onChange={(e) => setBody(e.target.value)}
              required
              rows={6}
              maxLength={10000}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-y"
              placeholder="Skriv din besked her..."
            />
          </div>
          {previewMutation.isError && (
            <p className="text-sm text-red-600">Noget gik galt. Prøv igen.</p>
          )}
          <button
            type="submit"
            disabled={previewMutation.isPending}
            className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors disabled:opacity-50"
          >
            {previewMutation.isPending ? 'Henter modtagere...' : 'Fortsæt'}
          </button>
        </form>
      )}

      {step === 'confirm' && (
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-5">
          <h2 className="font-semibold text-gray-900">Bekræft udsendelse</h2>
          <div className="bg-gray-50 rounded-lg p-4 space-y-2 text-sm text-gray-700">
            <div>
              <span className="font-medium">Modtagere: </span>
              {classId
                ? (classes.find((c) => c.id === classId)?.name ?? 'Valgt klasse')
                : 'Hele skolen'}
            </div>
            <div>
              <span className="font-medium">Emne: </span>
              {subject}
            </div>
            <div>
              <span className="font-medium">Antal forældre: </span>
              <span className="font-semibold text-brand-700">{recipientCount}</span>
            </div>
          </div>
          {recipientCount === 0 && (
            <p className="text-sm text-yellow-700 bg-yellow-50 border border-yellow-200 rounded-lg px-4 py-3">
              Ingen forældre med bekræftet e-mailadresse fundet. E-mailen sendes ikke.
            </p>
          )}
          {sendMutation.isError && (
            <p className="text-sm text-red-600">Noget gik galt under afsendelse. Prøv igen.</p>
          )}
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => setStep('compose')}
              disabled={sendMutation.isPending}
              className="px-4 py-2 text-sm font-medium text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              Tilbage
            </button>
            <button
              type="button"
              onClick={handleSend}
              disabled={sendMutation.isPending}
              className="px-5 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors disabled:opacity-50"
            >
              {sendMutation.isPending ? 'Sender...' : 'Send e-mail'}
            </button>
          </div>
        </div>
      )}

      {step === 'done' && (
        <div className="bg-white border border-gray-200 rounded-xl p-6 space-y-4 text-center">
          <div className="mx-auto h-12 w-12 rounded-full bg-green-100 flex items-center justify-center">
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              className="text-green-600"
            >
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </div>
          <p className="text-sm font-medium text-gray-900">E-mail sendt!</p>
          <button
            type="button"
            onClick={handleReset}
            className="px-4 py-2 text-sm bg-brand-600 text-white rounded-lg hover:bg-brand-700 transition-colors"
          >
            Send ny e-mail
          </button>
        </div>
      )}

      {isAdmin && log.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-base font-semibold text-gray-900">Sendte e-mails</h2>
          <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 border-b border-gray-200">
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">Dato</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">
                    Afsender
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">Klasse</th>
                  <th className="px-4 py-2 text-left text-xs font-medium text-gray-500">Emne</th>
                  <th className="px-4 py-2 text-right text-xs font-medium text-gray-500">Antal</th>
                </tr>
              </thead>
              <tbody>
                {log.map((entry) => (
                  <tr key={entry.id} className="border-b border-gray-100 last:border-0">
                    <td className="px-4 py-2 text-gray-500 whitespace-nowrap">
                      {entry.sentAt
                        ? new Date(entry.sentAt).toLocaleDateString('da-DK', {
                            day: 'numeric',
                            month: 'short',
                            year: 'numeric',
                          })
                        : ''}
                    </td>
                    <td className="px-4 py-2 text-gray-700">{entry.senderName}</td>
                    <td className="px-4 py-2 text-gray-700">{entry.className ?? 'Hele skolen'}</td>
                    <td className="px-4 py-2 text-gray-900 max-w-xs truncate">{entry.subject}</td>
                    <td className="px-4 py-2 text-right text-gray-700">{entry.recipientCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
