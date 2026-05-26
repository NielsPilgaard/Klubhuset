import { useState, useEffect, useRef, useCallback } from 'react'
import keycloak from '../auth/keycloak'
import { usePageTitle } from '../hooks/usePageTitle'

type RecipientType = 'Parent' | 'Staff'

interface InboxMessageDto {
  id: string
  senderId: string
  senderType: RecipientType
  senderName: string
  subject: string
  body: string
  sentAt: string
  readAt?: string
}

interface SentMessageDto {
  id: string
  recipientId: string
  recipientType: RecipientType
  recipientName: string
  subject: string
  body: string
  sentAt: string
  readAt?: string
}

interface RecipientDto {
  id: string
  name: string
  type: RecipientType
  avatarUrl?: string
}

async function authFetch(url: string, options?: RequestInit): Promise<Response> {
  return fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${keycloak.token}`,
      ...options?.headers,
    },
  })
}

function formatRelativeTime(iso: string): string {
  const now = new Date()
  const date = new Date(iso)
  const diffMs = now.getTime() - date.getTime()
  const diffMin = Math.floor(diffMs / 60000)
  const diffH = Math.floor(diffMin / 60)
  const diffD = Math.floor(diffH / 24)

  if (diffMin < 1) {
    return 'Lige nu'
  }
  if (diffMin < 60) {
    return `${diffMin} min siden`
  }
  if (diffH < 24) {
    return `${diffH} t siden`
  }
  if (diffD === 1) {
    return 'I går'
  }
  if (diffD < 7) {
    return `${diffD} dage siden`
  }
  const dd = date.getDate().toString().padStart(2, '0')
  const mo = (date.getMonth() + 1).toString().padStart(2, '0')
  return `${dd}/${mo}`
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .map(n => n.charAt(0))
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

function truncate(text: string, max: number): string {
  if (text.length <= max) {
    return text
  }
  return text.slice(0, max) + '…'
}

export default function BeskederPage() {
  usePageTitle('Beskeder')

  const [tab, setTab] = useState<'inbox' | 'sent'>('inbox')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [composeOpen, setComposeOpen] = useState(false)

  const [inbox, setInbox] = useState<InboxMessageDto[]>([])
  const [sent, setSent] = useState<SentMessageDto[]>([])
  const [loading, setLoading] = useState(true)

  // Compose state
  const [recipientSearch, setRecipientSearch] = useState('')
  const [recipientResults, setRecipientResults] = useState<RecipientDto[]>([])
  const [selectedRecipient, setSelectedRecipient] = useState<RecipientDto | null>(null)
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [sending, setSending] = useState(false)
  const [sendError, setSendError] = useState<string | null>(null)
  const [showDropdown, setShowDropdown] = useState(false)

  const searchDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const fetchMessages = useCallback(async () => {
    setLoading(true)
    try {
      const [inboxRes, sentRes] = await Promise.all([
        authFetch('/api/v1/messages/inbox'),
        authFetch('/api/v1/messages/sent'),
      ])
      if (inboxRes.ok) {
        setInbox(await inboxRes.json())
      }
      if (sentRes.ok) {
        setSent(await sentRes.json())
      }
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchMessages()
  }, [fetchMessages])

  async function handleSelectInbox(id: string) {
    setSelectedId(id)
    const msg = inbox.find(m => m.id === id)
    if (msg && !msg.readAt) {
      await authFetch(`/api/v1/messages/${id}/read`, { method: 'POST' })
      setInbox(prev => prev.map(m => m.id === id ? { ...m, readAt: new Date().toISOString() } : m))
    }
  }

  function handleRecipientSearchChange(value: string) {
    setRecipientSearch(value)
    if (searchDebounceRef.current) {
      clearTimeout(searchDebounceRef.current)
    }
    if (value.length < 2) {
      setRecipientResults([])
      setShowDropdown(false)
      return
    }
    searchDebounceRef.current = setTimeout(async () => {
      const res = await authFetch(`/api/v1/messages/recipients?q=${encodeURIComponent(value)}`)
      if (res.ok) {
        const data: RecipientDto[] = await res.json()
        setRecipientResults(data)
        setShowDropdown(data.length > 0)
      }
    }, 300)
  }

  function handleSelectRecipient(recipient: RecipientDto) {
    setSelectedRecipient(recipient)
    setRecipientSearch('')
    setRecipientResults([])
    setShowDropdown(false)
  }

  function handleOpenCompose() {
    setSelectedRecipient(null)
    setRecipientSearch('')
    setRecipientResults([])
    setShowDropdown(false)
    setSubject('')
    setBody('')
    setSendError(null)
    setComposeOpen(true)
  }

  async function handleSend() {
    if (!selectedRecipient || !subject.trim() || !body.trim()) {
      return
    }
    setSending(true)
    setSendError(null)
    try {
      const res = await authFetch('/api/v1/messages', {
        method: 'POST',
        body: JSON.stringify({
          recipientId: selectedRecipient.id,
          recipientType: selectedRecipient.type,
          subject: subject.trim(),
          body: body.trim(),
        }),
      })
      if (!res.ok) {
        setSendError('Der opstod en fejl. Prøv igen.')
        return
      }
      setComposeOpen(false)
      await fetchMessages()
    } catch {
      setSendError('Der opstod en fejl. Prøv igen.')
    } finally {
      setSending(false)
    }
  }

  const currentMessages = tab === 'inbox' ? inbox : sent

  const selectedInboxMsg = tab === 'inbox' ? inbox.find(m => m.id === selectedId) : null
  const selectedSentMsg = tab === 'sent' ? sent.find(m => m.id === selectedId) : null
  const selectedMsg = selectedInboxMsg ?? selectedSentMsg

  return (
    <div className="flex flex-col h-full min-h-0">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-white shrink-0">
        <h1 className="font-display text-xl font-semibold text-gray-900">Beskeder</h1>
        <button
          onClick={handleOpenCompose}
          className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Ny besked
        </button>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-gray-200 bg-white shrink-0">
        <button
          onClick={() => { setTab('inbox'); setSelectedId(null) }}
          className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
            tab === 'inbox'
              ? 'border-brand-600 text-brand-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          Indbakke
        </button>
        <button
          onClick={() => { setTab('sent'); setSelectedId(null) }}
          className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
            tab === 'sent'
              ? 'border-brand-600 text-brand-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          Sendt
        </button>
      </div>

      {/* Content */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* Message list */}
        <div className={`w-full lg:w-80 shrink-0 border-r border-gray-200 bg-white flex flex-col overflow-y-auto ${selectedId ? 'hidden lg:flex' : 'flex'}`}>
          {loading && (
            <div className="flex items-center justify-center py-12 text-sm text-gray-500">
              Indlæser…
            </div>
          )}
          {!loading && currentMessages.length === 0 && (
            <div className="flex items-center justify-center py-12 px-4">
              <p className="text-sm text-gray-400 text-center">Ingen beskeder</p>
            </div>
          )}
          <div className="divide-y divide-gray-100">
            {tab === 'inbox' &&
              inbox.map(msg => {
                const isUnread = !msg.readAt
                return (
                  <button
                    key={msg.id}
                    onClick={() => handleSelectInbox(msg.id)}
                    className={`w-full text-left px-4 py-3 hover:bg-gray-50 transition-colors ${selectedId === msg.id ? 'bg-brand-50' : ''}`}
                  >
                    <div className="flex items-center justify-between mb-0.5">
                      <span className={`text-sm truncate ${isUnread ? 'font-semibold text-gray-900' : 'font-medium text-gray-700'}`}>
                        {msg.senderName}
                      </span>
                      <span className="text-xs text-gray-400 shrink-0 ml-2">{formatRelativeTime(msg.sentAt)}</span>
                    </div>
                    <p className={`text-sm truncate ${isUnread ? 'font-medium text-gray-800' : 'text-gray-600'}`}>
                      {msg.subject}
                    </p>
                    <p className="text-xs text-gray-400 truncate mt-0.5">{truncate(msg.body, 80)}</p>
                  </button>
                )
              })
            }
            {tab === 'sent' &&
              sent.map(msg => (
                <button
                  key={msg.id}
                  onClick={() => setSelectedId(msg.id)}
                  className={`w-full text-left px-4 py-3 hover:bg-gray-50 transition-colors ${selectedId === msg.id ? 'bg-brand-50' : ''}`}
                >
                  <div className="flex items-center justify-between mb-0.5">
                    <span className="text-sm font-medium text-gray-700 truncate">Til: {msg.recipientName}</span>
                    <span className="text-xs text-gray-400 shrink-0 ml-2">{formatRelativeTime(msg.sentAt)}</span>
                  </div>
                  <p className="text-sm text-gray-600 truncate">{msg.subject}</p>
                  <p className="text-xs text-gray-400 truncate mt-0.5">{truncate(msg.body, 80)}</p>
                </button>
              ))
            }
          </div>
        </div>

        {/* Detail panel */}
        <div className={`flex-1 bg-gray-50 min-w-0 flex flex-col ${selectedId ? 'flex' : 'hidden lg:flex'}`}>
          {selectedMsg ? (
            <div className="flex flex-col h-full">
              {/* Back button (mobile) */}
              <div className="lg:hidden px-4 pt-3">
                <button
                  onClick={() => setSelectedId(null)}
                  className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="15 18 9 12 15 6" />
                  </svg>
                  Tilbage
                </button>
              </div>

              <div className="flex-1 overflow-y-auto px-6 py-6">
                <div className="max-w-2xl">
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">{selectedMsg.subject}</h2>

                  <div className="flex items-start gap-3 mb-6 pb-4 border-b border-gray-200">
                    <div className="flex items-center justify-center h-9 w-9 rounded-full bg-brand-100 text-brand-700 text-sm font-semibold shrink-0">
                      {tab === 'inbox'
                        ? getInitials((selectedMsg as InboxMessageDto).senderName)
                        : getInitials((selectedMsg as SentMessageDto).recipientName)}
                    </div>
                    <div className="min-w-0">
                      {tab === 'inbox' ? (
                        <>
                          <p className="text-sm font-medium text-gray-900">{(selectedMsg as InboxMessageDto).senderName}</p>
                          <p className="text-xs text-gray-500">
                            {(selectedMsg as InboxMessageDto).senderType === 'Parent' ? 'Forælder' : 'Medarbejder'}
                            {' · '}
                            {formatRelativeTime(selectedMsg.sentAt)}
                          </p>
                        </>
                      ) : (
                        <>
                          <p className="text-sm font-medium text-gray-900">Til: {(selectedMsg as SentMessageDto).recipientName}</p>
                          <p className="text-xs text-gray-500">
                            {(selectedMsg as SentMessageDto).recipientType === 'Parent' ? 'Forælder' : 'Medarbejder'}
                            {' · '}
                            {formatRelativeTime(selectedMsg.sentAt)}
                          </p>
                        </>
                      )}
                    </div>
                  </div>

                  <div className="text-sm text-gray-800 whitespace-pre-wrap leading-relaxed">
                    {selectedMsg.body}
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex-1 flex items-center justify-center">
              <div className="text-center">
                <svg className="mx-auto mb-3 text-gray-300" width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                  <polyline points="22,6 12,13 2,6" />
                </svg>
                <p className="text-sm text-gray-400">Vælg en besked for at læse den</p>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Compose modal */}
      {composeOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4">
          <div className="max-w-lg w-full bg-white rounded-xl p-6 shadow-xl">
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-base font-semibold text-gray-900">Ny besked</h2>
              <button
                onClick={() => setComposeOpen(false)}
                className="p-1 rounded text-gray-400 hover:text-gray-600 transition-colors"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>

            {/* Recipient field */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Modtager</label>
              {selectedRecipient ? (
                <div className="flex items-center gap-2 px-3 py-2 border border-gray-300 rounded-lg">
                  <div className="flex items-center justify-center h-6 w-6 rounded-full bg-brand-100 text-brand-700 text-xs font-semibold shrink-0">
                    {getInitials(selectedRecipient.name)}
                  </div>
                  <span className="text-sm text-gray-900 flex-1">{selectedRecipient.name}</span>
                  <span className="text-xs text-gray-400">
                    {selectedRecipient.type === 'Parent' ? 'Forælder' : 'Medarbejder'}
                  </span>
                  <button
                    onClick={() => setSelectedRecipient(null)}
                    className="ml-1 text-gray-400 hover:text-gray-600"
                  >
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="18" y1="6" x2="6" y2="18" />
                      <line x1="6" y1="6" x2="18" y2="18" />
                    </svg>
                  </button>
                </div>
              ) : (
                <div className="relative">
                  <input
                    type="text"
                    value={recipientSearch}
                    onChange={e => handleRecipientSearchChange(e.target.value)}
                    placeholder="Søg efter navn (min. 2 tegn)…"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                    onFocus={() => { if (recipientResults.length > 0) setShowDropdown(true) }}
                    onBlur={() => setTimeout(() => setShowDropdown(false), 150)}
                  />
                  {showDropdown && (
                    <div className="absolute z-10 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-y-auto">
                      {recipientResults.map(r => (
                        <button
                          key={r.id}
                          onMouseDown={() => handleSelectRecipient(r)}
                          className="flex items-center gap-3 w-full text-left px-3 py-2.5 hover:bg-gray-50 transition-colors"
                        >
                          <div className="flex items-center justify-center h-7 w-7 rounded-full bg-brand-100 text-brand-700 text-xs font-semibold shrink-0">
                            {r.avatarUrl
                              ? <img src={r.avatarUrl} alt="" className="h-7 w-7 rounded-full object-cover" />
                              : getInitials(r.name)}
                          </div>
                          <div className="min-w-0">
                            <p className="text-sm font-medium text-gray-900 truncate">{r.name}</p>
                            <p className="text-xs text-gray-500">{r.type === 'Parent' ? 'Forælder' : 'Medarbejder'}</p>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Subject */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">Emne</label>
              <input
                type="text"
                value={subject}
                onChange={e => setSubject(e.target.value)}
                placeholder="Emne"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>

            {/* Body */}
            <div className="mb-5">
              <label className="block text-sm font-medium text-gray-700 mb-1">Besked</label>
              <textarea
                value={body}
                onChange={e => setBody(e.target.value)}
                rows={6}
                placeholder="Skriv din besked her…"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
              />
            </div>

            {sendError && (
              <p className="text-sm text-red-600 mb-3">{sendError}</p>
            )}

            <div className="flex items-center justify-end gap-3">
              <button
                onClick={() => setComposeOpen(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 transition-colors"
              >
                Annuller
              </button>
              <button
                onClick={handleSend}
                disabled={sending || !selectedRecipient || !subject.trim() || !body.trim()}
                className="px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors disabled:opacity-50"
              >
                {sending ? 'Sender…' : 'Send'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
