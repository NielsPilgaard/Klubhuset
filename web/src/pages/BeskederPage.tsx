import { useCallback, useEffect, useRef, useState } from 'react'
import {
  getApiV1MessagesInbox,
  getApiV1MessagesRecipients,
  getApiV1MessagesSent,
  postApiV1Messages,
  postApiV1MessagesByIdRead,
} from '../api/generated/sdk.gen'
import { Modal } from '../components/Modal'
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
    .map((n) => n.charAt(0))
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

function truncate(text: string, max: number): string {
  if (text.length <= max) {
    return text
  }
  return `${text.slice(0, max)}…`
}

export default function BeskederPage() {
  usePageTitle('Beskeder')

  const [tab, setTab] = useState<'inbox' | 'sent'>('inbox')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [composeOpen, setComposeOpen] = useState(false)

  const [inbox, setInbox] = useState<InboxMessageDto[]>([])
  const [sent, setSent] = useState<SentMessageDto[]>([])
  const [loading, setLoading] = useState(true)

  // Directory state
  const [directorySearch, setDirectorySearch] = useState('')
  const [allRecipients, setAllRecipients] = useState<RecipientDto[]>([])
  const [filteredRecipients, setFilteredRecipients] = useState<RecipientDto[]>([])
  const [directoryLoading, setDirectoryLoading] = useState(true)
  // On mobile: which panel is shown — 'directory' | 'list' | 'detail'
  const [mobilePanel, setMobilePanel] = useState<'directory' | 'list' | 'detail'>('directory')

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
  const directoryDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    return () => {
      if (searchDebounceRef.current) {
        clearTimeout(searchDebounceRef.current)
        searchDebounceRef.current = null
      }
      if (directoryDebounceRef.current) {
        clearTimeout(directoryDebounceRef.current)
        directoryDebounceRef.current = null
      }
    }
  }, [])

  const fetchMessages = useCallback(async () => {
    setLoading(true)
    try {
      const [inboxRes, sentRes] = await Promise.all([
        getApiV1MessagesInbox({ throwOnError: false }),
        getApiV1MessagesSent({ throwOnError: false }),
      ])
      if (inboxRes.data) setInbox(inboxRes.data as InboxMessageDto[])
      if (sentRes.data) setSent(sentRes.data as SentMessageDto[])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchMessages()
  }, [fetchMessages])

  // Load full directory on mount
  useEffect(() => {
    async function loadDirectory() {
      setDirectoryLoading(true)
      try {
        const { data } = await getApiV1MessagesRecipients({
          query: { q: '' },
          throwOnError: false,
        })
        const recipients = (data ?? []) as RecipientDto[]
        setAllRecipients(recipients)
        setFilteredRecipients(recipients)
      } finally {
        setDirectoryLoading(false)
      }
    }
    loadDirectory()
  }, [])

  function handleDirectorySearch(value: string) {
    setDirectorySearch(value)
    if (directoryDebounceRef.current) {
      clearTimeout(directoryDebounceRef.current)
    }
    const searchAtCall = value
    directoryDebounceRef.current = setTimeout(async () => {
      if (searchAtCall.length === 0) {
        setFilteredRecipients(allRecipients)
        return
      }
      const { data } = await getApiV1MessagesRecipients({
        query: { q: searchAtCall },
        throwOnError: false,
      })
      setDirectorySearch((current) => {
        if (current === searchAtCall) {
          setFilteredRecipients((data ?? []) as RecipientDto[])
        }
        return current
      })
    }, 300)
  }

  async function handleSelectInbox(id: string) {
    setSelectedId(id)
    setMobilePanel('detail')
    const msg = inbox.find((m) => m.id === id)
    if (msg && !msg.readAt) {
      await postApiV1MessagesByIdRead({ path: { id }, throwOnError: false })
      setInbox((prev) =>
        prev.map((m) => (m.id === id ? { ...m, readAt: new Date().toISOString() } : m))
      )
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
      const { data } = await getApiV1MessagesRecipients({
        query: { q: value },
        throwOnError: false,
      })
      if (data) {
        const results = data as RecipientDto[]
        setRecipientResults(results)
        setShowDropdown(results.length > 0)
      }
    }, 300)
  }

  function handleSelectRecipient(recipient: RecipientDto) {
    setSelectedRecipient(recipient)
    setRecipientSearch('')
    setRecipientResults([])
    setShowDropdown(false)
  }

  function handleOpenCompose(prefilledRecipient?: RecipientDto) {
    setSelectedRecipient(prefilledRecipient ?? null)
    setRecipientSearch('')
    setRecipientResults([])
    setShowDropdown(false)
    setSubject('')
    setBody('')
    setSendError(null)
    setComposeOpen(true)
  }

  function handleDirectoryContactClick(recipient: RecipientDto) {
    handleOpenCompose(recipient)
  }

  async function handleSend() {
    if (!selectedRecipient || !subject.trim() || !body.trim()) {
      return
    }
    setSending(true)
    setSendError(null)
    try {
      await postApiV1Messages({
        body: {
          recipientId: selectedRecipient.id,
          recipientType: selectedRecipient.type,
          subject: subject.trim(),
          body: body.trim(),
        },
        throwOnError: true,
      })
      setComposeOpen(false)
      await fetchMessages()
    } catch {
      setSendError('Der opstod en fejl. Prøv igen.')
    } finally {
      setSending(false)
    }
  }

  const currentMessages = tab === 'inbox' ? inbox : sent

  const selectedInboxMsg = tab === 'inbox' ? inbox.find((m) => m.id === selectedId) : null
  const selectedSentMsg = tab === 'sent' ? sent.find((m) => m.id === selectedId) : null
  const selectedMsg = selectedInboxMsg ?? selectedSentMsg

  return (
    <div className="flex flex-col h-full min-h-0">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-white shrink-0">
        <h1 className="font-display text-xl font-semibold text-gray-900">Beskeder</h1>
        <button
          type="button"
          onClick={() => handleOpenCompose()}
          className="flex items-center gap-2 px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          <svg
            aria-hidden="true"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Ny besked
        </button>
      </div>

      {/* Content */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* Directory panel */}
        <div
          className={`w-full lg:w-64 shrink-0 border-r border-gray-200 bg-white flex flex-col ${mobilePanel === 'directory' ? 'flex' : 'hidden lg:flex'}`}
        >
          <div className="px-3 py-3 border-b border-gray-100 shrink-0">
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 mb-2">
              Kontakter
            </p>
            <div className="relative">
              <svg
                aria-hidden="true"
                className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400"
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <circle cx="11" cy="11" r="8" />
                <line x1="21" y1="21" x2="16.65" y2="16.65" />
              </svg>
              <input
                type="text"
                aria-label="Søg i kontakter"
                value={directorySearch}
                onChange={(e) => handleDirectorySearch(e.target.value)}
                placeholder="Søg…"
                className="w-full pl-8 pr-3 py-1.5 border border-gray-200 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
          </div>
          <div className="flex-1 overflow-y-auto divide-y divide-gray-50">
            {directoryLoading && (
              <div className="flex items-center justify-center py-8 text-sm text-gray-400">
                Indlæser…
              </div>
            )}
            {!directoryLoading && filteredRecipients.length === 0 && (
              <div className="flex items-center justify-center py-8 text-sm text-gray-400">
                Ingen kontakter
              </div>
            )}
            {filteredRecipients.map((r) => (
              <button
                type="button"
                key={r.id}
                onClick={() => handleDirectoryContactClick(r)}
                className="w-full text-left px-3 py-2.5 hover:bg-gray-50 transition-colors flex items-center gap-2.5"
              >
                <div className="flex items-center justify-center h-7 w-7 rounded-full bg-brand-100 text-brand-700 text-xs font-semibold shrink-0 overflow-hidden">
                  {r.avatarUrl ? (
                    <img src={r.avatarUrl} alt="" className="h-7 w-7 rounded-full object-cover" />
                  ) : (
                    getInitials(r.name)
                  )}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{r.name}</p>
                  <p className="text-xs text-gray-400">
                    {r.type === 'Parent' ? 'Forælder' : 'Medarbejder'}
                  </p>
                </div>
              </button>
            ))}
          </div>
          {/* Mobile: nav to message list */}
          <div className="lg:hidden px-3 py-2 border-t border-gray-100">
            <button
              type="button"
              onClick={() => setMobilePanel('list')}
              className="flex items-center gap-1 text-sm text-brand-600 font-medium"
            >
              Indbakke
              <svg
                aria-hidden="true"
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </button>
          </div>
        </div>

        {/* Message list */}
        <div
          className={`w-full lg:w-72 shrink-0 border-r border-gray-200 bg-white flex flex-col ${mobilePanel === 'list' ? 'flex' : 'hidden lg:flex'}`}
        >
          {/* Tabs */}
          <div className="flex border-b border-gray-200 shrink-0">
            <button
              type="button"
              onClick={() => {
                setTab('inbox')
                setSelectedId(null)
              }}
              className={`flex-1 px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
                tab === 'inbox'
                  ? 'border-brand-600 text-brand-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Indbakke
            </button>
            <button
              type="button"
              onClick={() => {
                setTab('sent')
                setSelectedId(null)
              }}
              className={`flex-1 px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
                tab === 'sent'
                  ? 'border-brand-600 text-brand-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Sendt
            </button>
          </div>

          <div className="flex-1 overflow-y-auto">
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
                inbox.map((msg) => {
                  const isUnread = !msg.readAt
                  return (
                    <button
                      type="button"
                      key={msg.id}
                      onClick={() => handleSelectInbox(msg.id)}
                      className={`w-full text-left px-4 py-3 hover:bg-gray-50 transition-colors ${selectedId === msg.id ? 'bg-brand-50' : ''}`}
                    >
                      <div className="flex items-center justify-between mb-0.5">
                        <span
                          className={`text-sm truncate ${isUnread ? 'font-semibold text-gray-900' : 'font-medium text-gray-700'}`}
                        >
                          {msg.senderName}
                        </span>
                        <span className="text-xs text-gray-400 shrink-0 ml-2">
                          {formatRelativeTime(msg.sentAt)}
                        </span>
                      </div>
                      <p
                        className={`text-sm truncate ${isUnread ? 'font-medium text-gray-800' : 'text-gray-600'}`}
                      >
                        {msg.subject}
                      </p>
                      <p className="text-xs text-gray-400 truncate mt-0.5">
                        {truncate(msg.body, 80)}
                      </p>
                    </button>
                  )
                })}
              {tab === 'sent' &&
                sent.map((msg) => (
                  <button
                    type="button"
                    key={msg.id}
                    onClick={() => {
                      setSelectedId(msg.id)
                      setMobilePanel('detail')
                    }}
                    className={`w-full text-left px-4 py-3 hover:bg-gray-50 transition-colors ${selectedId === msg.id ? 'bg-brand-50' : ''}`}
                  >
                    <div className="flex items-center justify-between mb-0.5">
                      <span className="text-sm font-medium text-gray-700 truncate">
                        Til: {msg.recipientName}
                      </span>
                      <span className="text-xs text-gray-400 shrink-0 ml-2">
                        {formatRelativeTime(msg.sentAt)}
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 truncate">{msg.subject}</p>
                    <p className="text-xs text-gray-400 truncate mt-0.5">
                      {truncate(msg.body, 80)}
                    </p>
                  </button>
                ))}
            </div>
          </div>

          {/* Mobile: back to directory */}
          <div className="lg:hidden px-3 py-2 border-t border-gray-100">
            <button
              type="button"
              onClick={() => setMobilePanel('directory')}
              className="flex items-center gap-1 text-sm text-gray-500"
            >
              <svg
                aria-hidden="true"
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="15 18 9 12 15 6" />
              </svg>
              Kontakter
            </button>
          </div>
        </div>

        {/* Detail panel */}
        <div
          className={`flex-1 bg-gray-50 min-w-0 flex flex-col ${mobilePanel === 'detail' ? 'flex' : 'hidden lg:flex'}`}
        >
          {selectedMsg ? (
            <div className="flex flex-col h-full">
              {/* Back button (mobile) */}
              <div className="lg:hidden px-4 pt-3">
                <button
                  type="button"
                  onClick={() => setMobilePanel('list')}
                  className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700"
                >
                  <svg
                    aria-hidden="true"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <polyline points="15 18 9 12 15 6" />
                  </svg>
                  Tilbage
                </button>
              </div>

              <div className="flex-1 overflow-y-auto px-6 py-6">
                <div className="max-w-2xl">
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">
                    {selectedMsg.subject}
                  </h2>

                  <div className="flex items-start gap-3 mb-6 pb-4 border-b border-gray-200">
                    <div className="flex items-center justify-center h-9 w-9 rounded-full bg-brand-100 text-brand-700 text-sm font-semibold shrink-0">
                      {tab === 'inbox'
                        ? getInitials((selectedMsg as InboxMessageDto).senderName)
                        : getInitials((selectedMsg as SentMessageDto).recipientName)}
                    </div>
                    <div className="min-w-0">
                      {tab === 'inbox' ? (
                        <>
                          <p className="text-sm font-medium text-gray-900">
                            {(selectedMsg as InboxMessageDto).senderName}
                          </p>
                          <p className="text-xs text-gray-500">
                            {(selectedMsg as InboxMessageDto).senderType === 'Parent'
                              ? 'Forælder'
                              : 'Medarbejder'}
                            {' · '}
                            {formatRelativeTime(selectedMsg.sentAt)}
                          </p>
                        </>
                      ) : (
                        <>
                          <p className="text-sm font-medium text-gray-900">
                            Til: {(selectedMsg as SentMessageDto).recipientName}
                          </p>
                          <p className="text-xs text-gray-500">
                            {(selectedMsg as SentMessageDto).recipientType === 'Parent'
                              ? 'Forælder'
                              : 'Medarbejder'}
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
                <svg
                  aria-hidden="true"
                  className="mx-auto mb-3 text-gray-300"
                  width="40"
                  height="40"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
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
      <Modal
        isOpen={composeOpen}
        onClose={() => setComposeOpen(false)}
        size="lg"
        contentClassName="bg-white rounded-xl p-6 shadow-xl w-full max-w-lg"
      >
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-base font-semibold text-gray-900">Ny besked</h2>
          <button
            type="button"
            aria-label="Luk"
            onClick={() => setComposeOpen(false)}
            className="p-1 rounded text-gray-400 hover:text-gray-600 transition-colors"
          >
            <svg
              aria-hidden="true"
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        {/* Recipient field */}
        <div className="mb-4">
          <label
            htmlFor="compose-recipient"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Modtager
          </label>
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
                type="button"
                aria-label="Fjern modtager"
                onClick={() => setSelectedRecipient(null)}
                className="ml-1 text-gray-400 hover:text-gray-600"
              >
                <svg
                  aria-hidden="true"
                  width="14"
                  height="14"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            </div>
          ) : (
            <div className="relative">
              <input
                id="compose-recipient"
                type="text"
                value={recipientSearch}
                onChange={(e) => handleRecipientSearchChange(e.target.value)}
                placeholder="Søg efter navn (min. 2 tegn)…"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                onFocus={() => {
                  if (recipientResults.length > 0) setShowDropdown(true)
                }}
                onBlur={() => setTimeout(() => setShowDropdown(false), 150)}
              />
              {showDropdown && (
                <div className="absolute z-10 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-y-auto">
                  {recipientResults.map((r) => (
                    <button
                      type="button"
                      key={r.id}
                      onMouseDown={() => handleSelectRecipient(r)}
                      className="flex items-center gap-3 w-full text-left px-3 py-2.5 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center justify-center h-7 w-7 rounded-full bg-brand-100 text-brand-700 text-xs font-semibold shrink-0">
                        {r.avatarUrl ? (
                          <img
                            src={r.avatarUrl}
                            alt=""
                            className="h-7 w-7 rounded-full object-cover"
                          />
                        ) : (
                          getInitials(r.name)
                        )}
                      </div>
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-gray-900 truncate">{r.name}</p>
                        <p className="text-xs text-gray-500">
                          {r.type === 'Parent' ? 'Forælder' : 'Medarbejder'}
                        </p>
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
          <label htmlFor="compose-subject" className="block text-sm font-medium text-gray-700 mb-1">
            Emne
          </label>
          <input
            id="compose-subject"
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            placeholder="Emne"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
        </div>

        {/* Body */}
        <div className="mb-5">
          <label htmlFor="compose-body" className="block text-sm font-medium text-gray-700 mb-1">
            Besked
          </label>
          <textarea
            id="compose-body"
            value={body}
            onChange={(e) => setBody(e.target.value)}
            rows={6}
            placeholder="Skriv din besked her…"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
          />
        </div>

        {sendError && <p className="text-sm text-red-600 mb-3">{sendError}</p>}

        <div className="flex items-center justify-end gap-3">
          <button
            type="button"
            onClick={() => setComposeOpen(false)}
            className="px-4 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 transition-colors"
          >
            Annuller
          </button>
          <button
            type="button"
            onClick={handleSend}
            disabled={sending || !selectedRecipient || !subject.trim() || !body.trim()}
            className="px-4 py-2 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors disabled:opacity-50"
          >
            {sending ? 'Sender…' : 'Send'}
          </button>
        </div>
      </Modal>
    </div>
  )
}
