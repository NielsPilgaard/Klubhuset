import { useState, useEffect, useRef, useCallback } from 'react'
import { useAuth } from '../auth/useAuth'
import type { NotificationType } from '../api/client'

interface NotificationItem {
  id: string
  type: NotificationType
  body: string
  createdAt: string
  readAt: string | null
  referenceId: string | null
}

function typeIcon(type: NotificationType): string {
  switch (type) {
    case 'AbsenceConfirmed': return '✅'
    case 'AbsenceDismissed': return '❌'
    case 'NewMessage': return '💬'
    case 'NewContactMessage': return '📖'
    case 'WeekPlanChanged': return '📅'
    default: return '🔔'
  }
}

function relativeTime(dateStr: string): string {
  const now = Date.now()
  const then = new Date(dateStr).getTime()
  const diff = Math.floor((now - then) / 1000)
  if (diff < 60) return 'Lige nu'
  if (diff < 3600) return `${Math.floor(diff / 60)} min. siden`
  if (diff < 86400) return `${Math.floor(diff / 3600)} t. siden`
  return `${Math.floor(diff / 86400)} dage siden`
}

export default function NotificationBell() {
  const { token } = useAuth()
  const [notifications, setNotifications] = useState<NotificationItem[]>([])
  const [open, setOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const fetchNotifications = useCallback(async () => {
    if (!token) return
    try {
      const res = await fetch('/api/v1/notifications', {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (res.ok) {
        const data: NotificationItem[] = await res.json()
        setNotifications(data)
      }
    } catch {
      // silently ignore fetch errors — bell is non-critical
    }
  }, [token])

  useEffect(() => {
    void fetchNotifications()
    const interval = setInterval(() => void fetchNotifications(), 60_000)
    return () => clearInterval(interval)
  }, [fetchNotifications])

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    if (open) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [open])

  const unreadCount = notifications.filter(n => n.readAt === null).length
  const displayed = notifications.slice(0, 10)

  async function markRead(id: string) {
    if (!token) return
    try {
      await fetch(`/api/v1/notifications/${id}/read`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      setNotifications(prev =>
        prev.map(n => n.id === id ? { ...n, readAt: new Date().toISOString() } : n)
      )
    } catch {
      // ignore
    }
  }

  async function markAllRead() {
    if (!token) return
    try {
      await fetch('/api/v1/notifications/read-all', {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      const now = new Date().toISOString()
      setNotifications(prev => prev.map(n => ({ ...n, readAt: n.readAt ?? now })))
    } catch {
      // ignore
    }
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setOpen(o => !o)}
        className="relative p-1.5 rounded text-gray-500 hover:text-gray-700 hover:bg-gray-100 transition-colors"
        aria-label="Notifikationer"
        data-testid="notification-bell"
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 flex items-center justify-center h-4 min-w-4 px-1 rounded-full bg-red-500 text-white text-[10px] font-bold leading-none">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 z-50 flex flex-col max-h-[80vh]">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100 shrink-0">
            <h3 className="text-sm font-semibold text-gray-800">Notifikationer</h3>
            {unreadCount > 0 && (
              <button
                onClick={() => void markAllRead()}
                className="text-xs text-brand-600 hover:text-brand-800 font-medium transition-colors"
                data-testid="mark-all-read"
              >
                Marker alle som læst
              </button>
            )}
          </div>

          {/* List */}
          <div className="overflow-y-auto flex-1">
            {displayed.length === 0 ? (
              <p className="px-4 py-6 text-sm text-gray-500 text-center">Ingen notifikationer</p>
            ) : (
              displayed.map(n => (
                <button
                  key={n.id}
                  onClick={() => void markRead(n.id)}
                  className={`w-full text-left px-4 py-3 border-b border-gray-50 hover:bg-gray-50 transition-colors flex gap-3 items-start ${
                    n.readAt === null ? 'bg-blue-50/60' : ''
                  }`}
                  data-testid="notification-item"
                >
                  <span className="text-lg shrink-0 mt-0.5">{typeIcon(n.type)}</span>
                  <div className="flex-1 min-w-0">
                    <p className={`text-sm leading-snug break-words ${n.readAt === null ? 'font-medium text-gray-900' : 'text-gray-700'}`}>
                      {n.body}
                    </p>
                    <p className="text-xs text-gray-400 mt-1">{relativeTime(n.createdAt)}</p>
                  </div>
                  {n.readAt === null && (
                    <span className="h-2 w-2 rounded-full bg-blue-500 shrink-0 mt-1.5" />
                  )}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  )
}
