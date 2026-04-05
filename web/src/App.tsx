import './App.css'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from './auth/AuthProvider'
import { useAuth } from './auth/useAuth'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import ClassesPage from './pages/ClassesPage'
import SchemaBuilderPage from './pages/SchemaBuilderPage'
import StaffPage from './pages/StaffPage'
import StaffSchedulePage from './pages/StaffSchedulePage'
import CoursesPage from './pages/CoursesPage'
import RoomsPage from './pages/RoomsPage'
import RoomSchedulePage from './pages/RoomSchedulePage'
import PrintSchemaPage from './pages/PrintSchemaPage'
import SkoleindstillingerPage from './pages/SkoleindstillingerPage'
import SignupPage from './pages/SignupPage'
import InvitationAcceptPage from './pages/InvitationAcceptPage'
import SchoolSetupWizardPage from './pages/SchoolSetupWizardPage'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import FilesPage from './pages/FilesPage'
import BillingPage from './pages/BillingPage'
import ExportsPage from './pages/ExportsPage'
import ClassTimeSlotsPage from './pages/ClassTimeSlotsPage'
import CalendarPage from './pages/CalendarPage'
import WeekPlanPage from './pages/WeekPlanPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

function HomeRedirect() {
  const { authenticated } = useAuth()
  if (authenticated) {
    return <Navigate to="/dashboard" replace />
  }
  return <LandingPage />
}

export default function App() {
  return (
    <AuthProvider>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public routes */}
          <Route path="/" element={<HomeRedirect />} />
          <Route path="login" element={<LoginPage />} />
          <Route path="signup" element={<SignupPage />} />
          <Route path="invitation/:token" element={<InvitationAcceptPage />} />
          <Route path="udskriv/klasse/:classId" element={<PrintSchemaPage />} />
          <Route path="udskriv/medarbejder/:staffId" element={<PrintSchemaPage />} />
          <Route path="udskriv/lokale/:roomId" element={<PrintSchemaPage />} />

          {/* Pages outside Layout (no sidebar) */}
          <Route path="setup" element={<SchoolSetupWizardPage />} />

          {/* Authenticated app */}
          <Route path="/" element={<Layout />}>
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="klasser" element={<ClassesPage />} />
            <Route path="klasser/:classId/skema/:schemaId" element={<SchemaBuilderPage />} />
            <Route path="klasser/:classId/lektioner" element={<ClassTimeSlotsPage />} />
            <Route path="klasser/:classId/schemas/:schemaId/lektioner" element={<ClassTimeSlotsPage />} />
            <Route path="medarbejdere" element={<StaffPage />} />
            <Route path="medarbejdere/:staffId/skema" element={<StaffSchedulePage />} />
            <Route path="fag" element={<CoursesPage />} />
            <Route path="lokaler" element={<RoomsPage />} />
            <Route path="lokaler/:roomId/skema" element={<RoomSchedulePage />} />
            <Route path="filer" element={<FilesPage />} />
            <Route path="eksporter" element={<ExportsPage />} />
            <Route path="abonnement" element={<BillingPage />} />
            <Route path="kalender" element={<CalendarPage />} />
            <Route path="klasser/:classId/ugeplan" element={<WeekPlanPage />} />
            <Route path="indstillinger" element={<SkoleindstillingerPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
    </AuthProvider>
  )
}
