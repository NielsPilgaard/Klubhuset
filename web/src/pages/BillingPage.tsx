import { useQuery, useMutation } from '@tanstack/react-query'
import {
  getApiV1BillingSubscriptionOptions,
  postApiV1BillingCheckoutMutation,
  postApiV1BillingPortalMutation,
} from '../api/generated/@tanstack/react-query.gen'
import type { SubscriptionDto } from '../api/generated/types.gen'
import { usePageTitle } from '../hooks/usePageTitle'

const SELF_SERVE_ENABLED = true

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('da-DK', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
}

function SkeletonStatusCard() {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-6 animate-pulse">
      <div className="h-5 w-48 bg-gray-200 rounded mb-3" />
      <div className="h-4 w-64 bg-gray-100 rounded mb-2" />
      <div className="h-4 w-40 bg-gray-100 rounded mb-5" />
      <div className="h-9 w-44 bg-gray-200 rounded-lg" />
    </div>
  )
}

function CheckIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      className="text-brand-500 shrink-0 mt-0.5"
    >
      <polyline points="20 6 9 17 4 12" />
    </svg>
  )
}

export default function BillingPage() {
  usePageTitle('Abonnement')
  const { data, isLoading, isError, refetch } = useQuery(getApiV1BillingSubscriptionOptions())

  const checkoutMutation = useMutation({
    ...postApiV1BillingCheckoutMutation(),
    onSuccess: (result) => {
      if (result?.url) window.location.href = result.url
    },
    onError: (error) => {
      const errorMessage = error instanceof Error ? error.message : 'Kunne ikke oprette checkoutsession'
      console.error('Checkout error:', error)
      alert(errorMessage)
    },
  })

  const portalMutation = useMutation({
    ...postApiV1BillingPortalMutation(),
    onSuccess: (result) => {
      if (result?.url) window.location.href = result.url
    },
    onError: (error) => {
      const errorMessage = error instanceof Error ? error.message : 'Kunne ikke åbne administrationsportal'
      console.error('Portal error:', error)
      alert(errorMessage)
    },
  })

  const isRedirecting = checkoutMutation.isPending || portalMutation.isPending

  return (
    <div className="p-6 lg:p-8 max-w-2xl mx-auto space-y-8">
      {/* Page header */}
      <div>
        <h1 className="font-display text-2xl font-semibold text-gray-900">Abonnement</h1>
        <p className="mt-1 text-sm text-gray-500">
          Administrer dit abonnement og betalingsoplysninger
        </p>
      </div>

      {/* Status card */}
      {isLoading ? (
        <SkeletonStatusCard />
      ) : isError ? (
        <div className="bg-red-50 border border-red-200 rounded-xl p-6 text-center">
          <p className="text-red-700 font-medium">Kunne ikke hente abonnementsoplysninger</p>
          <button
            onClick={() => refetch()}
            className="mt-3 px-4 py-2 text-sm bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors"
          >
            Prøv igen
          </button>
        </div>
      ) : data ? (
        <StatusCard
          data={data}
          onCheckout={() => checkoutMutation.mutate({})}
          onPortal={() => portalMutation.mutate({})}
          isRedirecting={isRedirecting}
        />
      ) : null}

      {/* Pricing info card */}
      <PricingCard
        isActive={data?.isActive ?? false}
        isTrialing={data?.isTrialing ?? false}
        trialEnd={data?.trialEnd}
        onCheckout={() => checkoutMutation.mutate({})}
        isRedirecting={isRedirecting}
      />
    </div>
  )
}

function StatusCard({
  data,
  onCheckout,
  onPortal,
  isRedirecting,
}: {
  data: SubscriptionDto
  onCheckout: () => void
  onPortal: () => void
  isRedirecting: boolean
}) {
  if (data.isTrialing) {
    return (
      <div className="bg-brand-50 border border-brand-200 rounded-xl p-6">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 flex-shrink-0 w-8 h-8 rounded-full bg-brand-100 flex items-center justify-center">
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              className="text-brand-600"
            >
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <h2 className="text-base font-semibold text-brand-900">Gratis prøveperiode</h2>
            <p className="mt-1 text-sm text-brand-700">
              {(data.trialDaysLeft ?? 0) > 0
                ? `${data.trialDaysLeft} ${data.trialDaysLeft === 1 ? 'dag' : 'dage'} tilbage — ingen betaling endnu`
                : 'Prøveperioden udløber i dag'}
            </p>
            <p className="mt-0.5 text-sm text-brand-600">
              Du kan bruge alle funktioner gratis frem til den {formatDate(data.trialEnd ?? '')}.
              Herefter koster det 299 kr/md.
            </p>
          </div>
        </div>
      </div>
    )
  }

  if (data.isActive) {
    return (
      <div className="bg-green-50 border border-green-200 rounded-xl p-6">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 flex-shrink-0 w-8 h-8 rounded-full bg-green-100 flex items-center justify-center">
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              className="text-green-600"
            >
              <polyline points="20 6 9 17 4 12" />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <h2 className="text-base font-semibold text-green-900">Aktivt abonnement — Basis (299 kr/md)</h2>
            {data.currentPeriodEnd && (
              <p className="mt-1 text-sm text-green-700">
                Næste betaling den {formatDate(data.currentPeriodEnd)}
              </p>
            )}
            <button
              onClick={onPortal}
              disabled={isRedirecting}
              className="mt-4 px-4 py-2 text-sm font-medium bg-green-700 text-white rounded-lg hover:bg-green-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isRedirecting ? 'Vent...' : 'Administrer abonnement'}
            </button>
            <p className="mt-2 text-xs text-green-600">
              Du kan opsige eller ændre dit abonnement via administreringsportalen.
            </p>
          </div>
        </div>
      </div>
    )
  }

  if (data.status === 'PastDue') {
    return (
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-6">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 flex-shrink-0 w-8 h-8 rounded-full bg-amber-100 flex items-center justify-center">
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              className="text-amber-600"
            >
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
              <line x1="12" y1="9" x2="12" y2="13" />
              <line x1="12" y1="17" x2="12.01" y2="17" />
            </svg>
          </div>
          <div className="flex-1 min-w-0">
            <h2 className="text-base font-semibold text-amber-900">Betaling mislykkedes</h2>
            <p className="mt-1 text-sm text-amber-700">
              Vi kunne ikke gennemføre betalingen. Opdater dine betalingsoplysninger for at fortsætte.
            </p>
            <button
              onClick={onPortal}
              disabled={isRedirecting}
              className="mt-4 px-4 py-2 text-sm font-medium bg-amber-600 text-white rounded-lg hover:bg-amber-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isRedirecting ? 'Vent...' : 'Opdater betalingskort'}
            </button>
          </div>
        </div>
      </div>
    )
  }

  // Canceled or Unpaid
  return (
    <div className="bg-red-50 border border-red-200 rounded-xl p-6">
      <div className="flex items-start gap-3">
        <div className="mt-0.5 flex-shrink-0 w-8 h-8 rounded-full bg-red-100 flex items-center justify-center">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            className="text-red-600"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="15" y1="9" x2="9" y2="15" />
            <line x1="9" y1="9" x2="15" y2="15" />
          </svg>
        </div>
        <div className="flex-1 min-w-0">
          <h2 className="text-base font-semibold text-red-900">Abonnement afsluttet</h2>
          <p className="mt-1 text-sm text-red-700">
            Dit abonnement er ikke aktivt. Forny for at få fuld adgang igen.
          </p>
          {SELF_SERVE_ENABLED && (
            <button
              onClick={onCheckout}
              disabled={isRedirecting}
              className="mt-4 px-4 py-2 text-sm font-medium bg-red-700 text-white rounded-lg hover:bg-red-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isRedirecting ? 'Vent...' : 'Forny abonnement'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

function PricingCard({
  isActive,
  isTrialing,
  trialEnd,
  onCheckout,
  isRedirecting,
}: {
  isActive?: boolean
  isTrialing?: boolean
  trialEnd?: string
  onCheckout: () => void
  isRedirecting: boolean
}) {
  const features = [
    'Ubegrænsede skemaer',
    'Op til 100 GB filer',
    'Konfliktkontrol – ingen dobbeltbookede lærere eller lokaler',
    'Support via e-mail',
    'Ingen binding',
  ]

  return (
    <div className={`bg-white rounded-xl border divide-y divide-gray-100 ${isActive || isTrialing ? 'border-brand-300 ring-1 ring-brand-200' : 'border-gray-200'}`}>
      <div className="px-6 py-5 flex items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-semibold text-gray-700">Basis</h2>
            {isActive && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-green-100 text-green-700">
                Aktiv
              </span>
            )}
            {isTrialing && !isActive && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-brand-100 text-brand-700">
                Prøveperiode
              </span>
            )}
          </div>
          <p className="mt-0.5 text-xs text-gray-400">Alt hvad din skole behøver</p>
        </div>
        <div className="text-right shrink-0">
          <span className="text-2xl font-semibold text-gray-900 tabular-nums">299</span>
          <span className="text-sm text-gray-500"> kr/md</span>
        </div>
      </div>
      <div className="px-6 py-5">
        <ul className="space-y-2.5">
          {features.map((feature) => (
            <li key={feature} className="flex items-start gap-2.5 text-sm text-gray-700">
              <CheckIcon />
              {feature}
            </li>
          ))}
        </ul>
      </div>
      {!isActive && SELF_SERVE_ENABLED && (
        <div className="px-6 py-4">
          <button
            onClick={onCheckout}
            disabled={isRedirecting}
            className="w-full px-4 py-2.5 text-sm font-medium bg-brand-600 text-white rounded-lg hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isRedirecting ? 'Vent...' : isTrialing && trialEnd ? `Abonner nu — første betaling den ${formatDate(trialEnd)}` : 'Køb abonnement'}
          </button>
        </div>
      )}
    </div>
  )
}
