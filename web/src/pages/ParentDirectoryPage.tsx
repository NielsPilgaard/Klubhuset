import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { usePageTitle } from '../hooks/usePageTitle'
import { getApiV1KontaktOptions } from '../api/generated/@tanstack/react-query.gen'
import type { KontaktControllerKontaktParentDto as KontaktParentDto } from '../api/generated/types.gen'

export default function ParentDirectoryPage() {
  usePageTitle('Kontakt')
  const [search, setSearch] = useState('')

  const { data: parents = [], isLoading } = useQuery({
    ...getApiV1KontaktOptions(),
    select: (data) => data as KontaktParentDto[],
  })

  const filtered = parents.filter(p =>
    (p.name ?? '').toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <div className="mb-6">
        <h1 className="font-display text-2xl font-semibold text-gray-900 mb-4">Kontakter</h1>
        <input
          type="search"
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Søg efter navn…"
          className="w-full max-w-sm px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
        />
      </div>

      {isLoading && (
        <div className="flex justify-center py-16">
          <div className="w-6 h-6 border-2 border-brand-600 border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {!isLoading && filtered.length === 0 && (
        <p className="text-sm text-gray-500 py-8">
          {search ? 'Ingen resultater for denne søgning.' : 'Ingen forældre at vise.'}
        </p>
      )}

      <div className="space-y-3">
        {filtered.map(parent => (
          <div key={parent.id} className="bg-white border border-gray-200 rounded-xl p-4 flex items-start gap-4">
            <div className="shrink-0">
              {parent.avatarUrl ? (
                <img
                  src={parent.avatarUrl}
                  alt=""
                  className="w-10 h-10 rounded-full object-cover"
                />
              ) : (
                <div className="w-10 h-10 rounded-full bg-brand-100 flex items-center justify-center">
                  <span className="text-sm font-semibold text-brand-700">
                    {(parent.name ?? '?').charAt(0).toUpperCase()}
                  </span>
                </div>
              )}
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-medium text-gray-900 text-sm">{parent.name}</p>
              {(parent.studentNames?.length ?? 0) > 0 && (
                <p className="text-xs text-gray-500 mt-0.5">
                  {parent.studentNames!.join(', ')}
                </p>
              )}
              {(parent.phone || parent.address || parent.city) && (
                <div className="mt-1.5 space-y-0.5">
                  {parent.phone && (
                    <p className="text-sm text-gray-700">
                      <a href={`tel:${parent.phone}`} className="hover:text-brand-600">{parent.phone}</a>
                    </p>
                  )}
                  {(parent.address || parent.city) && (
                    <p className="text-sm text-gray-600">
                      {[parent.address, parent.postalCode, parent.city].filter(Boolean).join(', ')}
                    </p>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
