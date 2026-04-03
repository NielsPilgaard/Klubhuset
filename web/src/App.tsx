import './App.css'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from './auth/AuthProvider'
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

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

export default function App() {
  return (
    <AuthProvider>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="klasser" element={<ClassesPage />} />
            <Route path="klasser/:classId/skema/:schemaId" element={<SchemaBuilderPage />} />
            <Route path="medarbejdere" element={<StaffPage />} />
            <Route path="medarbejdere/:staffId/skema" element={<StaffSchedulePage />} />
            <Route path="fag" element={<CoursesPage />} />
            <Route path="lokaler" element={<RoomsPage />} />
            <Route path="lokaler/:roomId/skema" element={<RoomSchedulePage />} />
          </Route>
          {/* Print pages — outside Layout (no sidebar) */}
          <Route path="udskriv/klasse/:classId" element={<PrintSchemaPage />} />
          <Route path="udskriv/medarbejder/:staffId" element={<PrintSchemaPage />} />
          <Route path="udskriv/lokale/:roomId" element={<PrintSchemaPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
    </AuthProvider>
  )
}
