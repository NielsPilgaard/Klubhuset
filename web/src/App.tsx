import './App.css'
import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import ScrollToTop from './components/ScrollToTop'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/useAuth'
import Layout from './components/Layout'

// Keep critical public pages as regular imports
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import SignupPage from './pages/SignupPage'
import InvitationAcceptPage from './pages/InvitationAcceptPage'
import OmPage from './pages/OmPage'
import KontaktPage from './pages/KontaktPage'

// Lazy load legal/info pages
const PrivatlivspolitikPage = lazy(() => import('./pages/PrivatlivspolitikPage'))

// Lazy load all other pages
const DashboardPage = lazy(() => import('./pages/DashboardPage'))
const MySchedulePage = lazy(() => import('./pages/MySchedulePage'))
const ClassesPage = lazy(() => import('./pages/ClassesPage'))
const SchemaBuilderPage = lazy(() => import('./pages/SchemaBuilderPage'))
const StaffPage = lazy(() => import('./pages/StaffPage'))
const StaffSchedulePage = lazy(() => import('./pages/StaffSchedulePage'))
const CoursesPage = lazy(() => import('./pages/CoursesPage'))
const RoomsPage = lazy(() => import('./pages/RoomsPage'))
const RoomSchedulePage = lazy(() => import('./pages/RoomSchedulePage'))
const PrintSchemaPage = lazy(() => import('./pages/PrintSchemaPage'))
const SkoleindstillingerPage = lazy(() => import('./pages/SkoleindstillingerPage'))
const SchoolSetupWizardPage = lazy(() => import('./pages/SchoolSetupWizardPage'))
const FilesPage = lazy(() => import('./pages/FilesPage'))
const BillingPage = lazy(() => import('./pages/BillingPage'))
const ExportsPage = lazy(() => import('./pages/ExportsPage'))
const ClassTimeSlotsPage = lazy(() => import('./pages/ClassTimeSlotsPage'))
const CalendarPage = lazy(() => import('./pages/CalendarPage'))
const WeekPlanPage = lazy(() => import('./pages/WeekPlanPage'))
const SfoPage = lazy(() => import('./pages/SfoPage'))
const StudentsPage = lazy(() => import('./pages/StudentsPage'))
const ParentsPage = lazy(() => import('./pages/ParentsPage'))
const YearRollPage = lazy(() => import('./pages/AarsrulPage'))
const ParentSchemaPage = lazy(() => import('./pages/parent/ParentSchemaPage'))
const ParentCalendarPage = lazy(() => import('./pages/parent/ParentCalendarPage'))
const ParentUgeplanPage = lazy(() => import('./pages/parent/ParentUgeplanPage'))

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 10_000,
    },
  },
})

function HomeRedirect() {
  const { authenticated, isAdmin, isParent } = useAuth()
  if (authenticated) {
    if (isParent) return <Navigate to="/foraeldrevisning/skema" replace />
    return <Navigate to={isAdmin ? '/dashboard' : '/mig/skema'} replace />
  }
  return <LandingPage />
}

function AdminRoute({ children }: { children: JSX.Element }) {
  const { isAdmin } = useAuth()
  if (!isAdmin) return <Navigate to="/mig/skema" replace />
  return <>{children}</>
}

function ParentRoute({ children }: { children: JSX.Element }) {
  const { isParent } = useAuth()
  if (!isParent) return <Navigate to="/mig/skema" replace />
  return <>{children}</>
}

export default function App() {
  return (
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <ScrollToTop />
          <Suspense fallback={<div className="flex items-center justify-center min-h-screen">Indlæser...</div>}>
            <Routes>
          {/* Public routes */}
          <Route path="/" element={<HomeRedirect />} />
          <Route path="login" element={<LoginPage />} />
          <Route path="signup" element={<SignupPage />} />
          <Route path="invitation/:token" element={<InvitationAcceptPage />} />
          <Route path="parent-invitation/:token" element={<InvitationAcceptPage />} />
          <Route path="om" element={<OmPage />} />
          <Route path="privatlivspolitik" element={<PrivatlivspolitikPage />} />
          <Route path="kontakt" element={<KontaktPage />} />
          <Route path="udskriv/klasse/:classId" element={<PrintSchemaPage />} />
          <Route path="udskriv/medarbejder/:staffId" element={<PrintSchemaPage />} />
          <Route path="udskriv/lokale/:roomId" element={<PrintSchemaPage />} />

          {/* Pages outside Layout (no sidebar) */}
          <Route path="setup" element={<SchoolSetupWizardPage />} />

          {/* Authenticated app */}
          <Route path="/" element={<Layout />}>
            <Route path="dashboard" element={<AdminRoute><DashboardPage /></AdminRoute>} />
            <Route path="mig/skema" element={<MySchedulePage />} />
            <Route path="klasser" element={<ClassesPage />} />
            <Route path="klasser/:classId/skema/:schemaId" element={<SchemaBuilderPage />} />
            <Route path="klasser/:classId/lektioner" element={<AdminRoute><ClassTimeSlotsPage /></AdminRoute>} />
            <Route path="klasser/:classId/schemas/:schemaId/lektioner" element={<ClassTimeSlotsPage />} />
            <Route path="medarbejdere" element={<AdminRoute><StaffPage /></AdminRoute>} />
            <Route path="medarbejdere/:staffId/skema" element={<AdminRoute><StaffSchedulePage /></AdminRoute>} />
            <Route path="fag" element={<CoursesPage />} />
            <Route path="lokaler" element={<RoomsPage />} />
            <Route path="lokaler/:roomId/skema" element={<RoomSchedulePage />} />
            <Route path="filer" element={<FilesPage />} />
            <Route path="eksporter" element={<AdminRoute><ExportsPage /></AdminRoute>} />
            <Route path="abonnement" element={<AdminRoute><BillingPage /></AdminRoute>} />
            <Route path="kalender" element={<CalendarPage />} />
            <Route path="klasser/:classId/ugeplan" element={<WeekPlanPage />} />
            <Route path="indstillinger" element={<AdminRoute><SkoleindstillingerPage /></AdminRoute>} />
            <Route path="sfo" element={<AdminRoute><SfoPage /></AdminRoute>} />
            <Route path="elever" element={<AdminRoute><StudentsPage /></AdminRoute>} />
            <Route path="foraeldre" element={<AdminRoute><ParentsPage /></AdminRoute>} />
            <Route path="aarsrul" element={<AdminRoute><YearRollPage /></AdminRoute>} />
            <Route path="foraeldrevisning/skema" element={<ParentRoute><ParentSchemaPage /></ParentRoute>} />
            <Route path="foraeldrevisning/kalender" element={<ParentRoute><ParentCalendarPage /></ParentRoute>} />
            <Route path="foraeldrevisning/ugeplan" element={<ParentRoute><ParentUgeplanPage /></ParentRoute>} />
          </Route>
        </Routes>
          </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
    </AuthProvider>
  )
}
