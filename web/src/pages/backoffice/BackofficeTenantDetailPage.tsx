import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, Link } from 'react-router-dom'
import { api } from '../../api/client'
import type { SubscriptionStatus } from '../../api/client'

type ModuleItem = {
  module: 'ParentModule' | 'BoardModule'
  isAdminOverride: boolean
  stripeSubscriptionItemId: string | null
}

type TenantDetail = {
  id: string
  name: string
  contactEmail: string | null
  contactPhone: string | null
  createdAt: string
  subscriptionStatus: SubscriptionStatus
  stripeCustomerId: string | null
  stripeSubscriptionId: string | null
  trialEnd: string
  currentPeriodEnd: string | null
  modules: ModuleItem[]
}

const ALL_MODULES: ModuleItem['module'][] = ['ParentModule', 'BoardModule']

const moduleLabels: Record<ModuleItem['module'], string> = {
  ParentModule: 'Forældremodul',
  BoardModule: 'Bestyrelsesmodul',
}

const statusLabel: Record<SubscriptionStatus, string> = {
  Trialing: 'Prøveperiode',
  Active: 'Aktiv',
  PastDue: 'Forfaldent',
  Canceled: 'Annulleret',
  Unpaid: 'Ubetalt',
}

const statusColors: Record<SubscriptionStatus, string> = {
  Trialing: 'bg-blue-100 text-blue-700',
  Active: 'bg-green-100 text-green-700',
  PastDue: 'bg-yellow-100 text-yellow-800',
  Canceled: 'bg-red-100 text-red-700',
  Unpaid: 'bg-red-100 text-red-700',
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('da-DK', { day: 'numeric', month: 'short', year: 'numeric' })
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</dt>
      <dd className="mt-0.5 text-sm text-gray-900">{value ?? '—'}</dd>
    </div>
  )
}

export default function BackofficeTenantDetailPage() {
  const { schoolId } = useParams<{ schoolId: string }>()
  const qc = useQueryClient()
  const queryKey = ['admin', 'tenants', schoolId]

  const { data: tenant, isLoading, isError } = useQuery({
    queryKey,
    queryFn: () => api.get<TenantDetail>(`/admin/tenants/${schoolId}`),
    enabled: !!schoolId,
  })

  const grantMutation = useMutation({
    mutationFn: (module: ModuleItem['module']) =>
      api.post<void>(`/admin/tenants/${schoolId}/modules`, { module }),
    onSuccess: () => qc.invalidateQueries({ queryKey }),
  })

  const revokeMutation = useMutation({
    mutationFn: (module: ModuleItem['module']) =>
      api.delete(`/admin/tenants/${schoolId}/modules/${module}`),
    onSuccess: () => qc.invalidateQueries({ queryKey }),
  })

  if (isLoading) return <div className="text-gray-500 text-sm">Henter skole…</div>
  if (isError || !tenant) return <div className="text-red-600 text-sm">Kunne ikke hente skole.</div>

  const activeModules = new Set(tenant.modules.map((m) => m.module))

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center gap-3">
        <Link to="/backoffice/tenants" className="text-sm text-gray-400 hover:text-gray-600">
          ← Alle skoler
        </Link>
        <span className="text-gray-300">/</span>
        <span className="text-sm font-medium text-gray-900">{tenant.name}</span>
      </div>

      {/* School info */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Skoleoplysninger</h2>
        <dl className="grid grid-cols-2 gap-4">
          <Field label="Navn" value={tenant.name} />
          <Field label="Oprettet" value={formatDate(tenant.createdAt)} />
          <Field label="E-mail" value={tenant.contactEmail} />
          <Field label="Telefon" value={tenant.contactPhone} />
        </dl>
      </div>

      {/* Subscription */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Abonnement</h2>
        <dl className="grid grid-cols-2 gap-4">
          <div>
            <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">Status</dt>
            <dd className="mt-0.5">
              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[tenant.subscriptionStatus]}`}>
                {statusLabel[tenant.subscriptionStatus]}
              </span>
            </dd>
          </div>
          <Field label="Prøveperiode slut" value={formatDate(tenant.trialEnd)} />
          <Field
            label="Faktureringsperiode slut"
            value={tenant.currentPeriodEnd ? formatDate(tenant.currentPeriodEnd) : null}
          />
          <div />
          <div className="col-span-2">
            <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">Stripe kunde-ID</dt>
            <dd className="mt-0.5 text-sm font-mono text-gray-700">{tenant.stripeCustomerId ?? '—'}</dd>
          </div>
          <div className="col-span-2">
            <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">Stripe abonnements-ID</dt>
            <dd className="mt-0.5 text-sm font-mono text-gray-700">{tenant.stripeSubscriptionId ?? '—'}</dd>
          </div>
        </dl>
      </div>

      {/* Modules */}
      <div className="bg-white border border-gray-200 rounded-xl p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">Moduler</h2>
        <div className="space-y-3">
          {ALL_MODULES.map((mod) => {
            const active = activeModules.has(mod)
            const item = tenant.modules.find((m) => m.module === mod)
            const isPending =
              grantMutation.isPending || revokeMutation.isPending

            return (
              <div
                key={mod}
                className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0"
              >
                <div className="flex items-center gap-3">
                  <span
                    className={`w-2 h-2 rounded-full ${active ? 'bg-green-500' : 'bg-gray-300'}`}
                  />
                  <span className="text-sm font-medium text-gray-900">{moduleLabels[mod]}</span>
                  {item && (
                    <span
                      className={`text-xs px-1.5 py-0.5 rounded font-medium ${
                        item.isAdminOverride
                          ? 'bg-purple-100 text-purple-700'
                          : 'bg-indigo-100 text-indigo-700'
                      }`}
                    >
                      {item.isAdminOverride ? 'Admin override' : 'Stripe'}
                    </span>
                  )}
                </div>
                <button
                  disabled={isPending}
                  onClick={() => {
                    if (active) {
                      revokeMutation.mutate(mod)
                    } else {
                      grantMutation.mutate(mod)
                    }
                  }}
                  className={`text-sm px-3 py-1 rounded-lg font-medium transition-colors disabled:opacity-50 ${
                    active
                      ? 'bg-red-50 text-red-600 hover:bg-red-100'
                      : 'bg-indigo-50 text-indigo-700 hover:bg-indigo-100'
                  }`}
                >
                  {active ? 'Fjern' : 'Tildel'}
                </button>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
