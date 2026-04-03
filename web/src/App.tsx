import './App.css'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import Layout from './components/Layout'
import DashboardPage from './pages/DashboardPage'
import ClassesPage from './pages/ClassesPage'
import SchemaBuilderPage from './pages/SchemaBuilderPage'
import StaffPage from './pages/StaffPage'
import CoursesPage from './pages/CoursesPage'
import RoomsPage from './pages/RoomsPage'

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
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="klasser" element={<ClassesPage />} />
            <Route path="klasser/:classId/skema/:schemaId" element={<SchemaBuilderPage />} />
            <Route path="medarbejdere" element={<StaffPage />} />
            <Route path="fag" element={<CoursesPage />} />
            <Route path="lokaler" element={<RoomsPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
