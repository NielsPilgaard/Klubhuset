import type React from 'react'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  size?: 'sm' | 'md' | 'lg'
  contentClassName?: string
  backdropClassName?: string
  onKeyDown?: (e: React.KeyboardEvent) => void
  children: React.ReactNode
}

const sizeClass = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
}

export function Modal({
  isOpen,
  onClose,
  title,
  size = 'md',
  contentClassName,
  backdropClassName,
  onKeyDown,
  children,
}: ModalProps) {
  if (!isOpen) return null

  return (
    <button
      type="button"
      tabIndex={-1}
      aria-label="Luk"
      className={
        backdropClassName ?? 'fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 text-left'
      }
      onClick={onClose}
      onKeyDown={onKeyDown}
    >
      <div
        role="dialog"
        aria-modal="true"
        className={contentClassName ?? `bg-white rounded-2xl shadow-xl w-full ${sizeClass[size]}`}
        onClick={(e) => e.stopPropagation()}
      >
        {title && (
          <div className="px-6 py-5 border-b border-gray-100">
            <h2 className="font-display text-lg font-semibold text-gray-900">{title}</h2>
          </div>
        )}
        {children}
      </div>
    </button>
  )
}
