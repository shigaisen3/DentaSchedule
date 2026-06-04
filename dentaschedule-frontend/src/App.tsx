import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './store/AuthContext';
import PrivateRoute from './components/PrivateRoute';
import RoleGuard from './components/RoleGuard';
import AdminLayout from './components/AdminLayout';
import Spinner from './components/ui/Spinner';

// Lazy-loaded pages (code-splitting per spec)
const BookingWizard = lazy(() => import('./features/booking/BookingWizard'));
const CheckAppointmentPage = lazy(() => import('./features/booking/CheckAppointmentPage'));
const ScheduleView = lazy(() => import('./features/schedule/ScheduleView'));
const LoginPage = lazy(() => import('./features/admin/LoginPage'));
const DashboardPage = lazy(() => import('./features/admin/DashboardPage'));
const AppointmentsPage = lazy(() => import('./features/admin/appointments/AppointmentsPage'));
const ClinicsPage = lazy(() => import('./features/admin/clinics/ClinicsPage'));
const DoctorsPage = lazy(() => import('./features/admin/doctors/DoctorsPage'));
const DoctorSchedulePage = lazy(() => import('./features/admin/doctors/DoctorSchedulePage'));
const UsersPage = lazy(() => import('./features/admin/users/UsersPage'));

const queryClient = new QueryClient();

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Suspense fallback={<Spinner />}>
            <Routes>
              {/* Public routes */}
              <Route path="/" element={<Navigate to="/booking" replace />} />
              <Route path="/booking" element={<BookingWizard />} />
              <Route path="/check" element={<CheckAppointmentPage />} />
              <Route path="/schedule/:clinicId/:doctorId" element={<ScheduleView />} />
              <Route path="/login" element={<LoginPage />} />

              {/* Protected admin routes */}
              <Route element={<PrivateRoute />}>
                <Route element={<AdminLayout />}>
                  {/* Assistant + Admin */}
                  <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
                  <Route path="/admin/dashboard" element={<DashboardPage />} />
                  <Route path="/admin/appointments" element={<AppointmentsPage />} />

                  {/* Admin only */}
                  <Route element={<RoleGuard roles={['Admin']} />}>
                    <Route path="/admin/clinics" element={<ClinicsPage />} />
                    <Route path="/admin/doctors" element={<DoctorsPage />} />
                    <Route path="/admin/doctors/:id/schedule" element={<DoctorSchedulePage />} />
                    <Route path="/admin/users" element={<UsersPage />} />
                  </Route>
                </Route>
              </Route>

              {/* Catch-all */}
              <Route path="*" element={<Navigate to="/booking" replace />} />
            </Routes>
          </Suspense>
          <Toaster position="top-right" />
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
