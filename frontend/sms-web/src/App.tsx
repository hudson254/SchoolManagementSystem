import { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { SnackbarProvider } from 'notistack';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider as CustomThemeProvider } from './contexts/ThemeContext';
import { theme } from './theme';
import { ProtectedRoute } from './components/Common/ProtectedRoute';
import { Layout } from './components/Layout/Layout';
import { ErrorBoundary } from './components/Common/ErrorBoundary';

// Lazy load pages (pages use named exports, so map them to default for React.lazy)
const loadPage = (importFn: () => Promise<any>, componentName?: string) =>
  lazy(() => importFn().then((m) => ({ default: componentName ? m[componentName] : (m.default || m) })));

const Login = loadPage(() => import('./pages/Login'), 'Login');
const Register = loadPage(() => import('./pages/Register'), 'Register');
const Dashboard = loadPage(() => import('./pages/Dashboard'), 'Dashboard');
const Students = loadPage(() => import('./pages/Students'), 'Students');
const StudentDetail = loadPage(() => import('./pages/StudentDetail'), 'StudentDetail');
const Lecturers = loadPage(() => import('./pages/Lecturers'), 'Lecturers');
const Courses = loadPage(() => import('./pages/Courses'), 'Courses');
const CourseOfferings = loadPage(() => import('./pages/CourseOfferings'), 'CourseOfferings');
const CourseOfferingDetail = loadPage(() => import('./pages/CourseOfferingDetail'), 'CourseOfferingDetail');
const CourseOfferingFormPage = loadPage(() => import('./pages/CourseOfferingFormPage'), 'CourseOfferingFormPage');
const Units = loadPage(() => import('./pages/Units'), 'Units');
const Timetable = loadPage(() => import('./pages/Timetable'), 'Timetable');
const Accommodation = loadPage(() => import('./pages/Accommodation'), 'Accommodation');
const Assignments = loadPage(() => import('./pages/Assignments'), 'Assignments');
const Grades = loadPage(() => import('./pages/Grades'), 'Grades');
const Reports = loadPage(() => import('./pages/Reports'), 'Reports');
const Users = loadPage(() => import('./pages/Users'), 'Users');
const Settings = loadPage(() => import('./pages/Settings'), 'Settings');
const Profile = loadPage(() => import('./pages/Profile'), 'Profile');
const Calendar = loadPage(() => import('./pages/Calendar'), 'Calendar');
const Certificates = loadPage(() => import('./pages/Certificates'), 'Certificates');
const MyCertificates = loadPage(() => import('./pages/MyCertificates'), 'MyCertificates');
const CertificateVerification = loadPage(() => import('./pages/CertificateVerification'), 'CertificateVerification');
const CertificateTemplates = loadPage(() => import('./pages/CertificateTemplates'), 'CertificateTemplates');
const CourseSelectionPage = loadPage(() => import('./pages/CourseSelectionPage'), 'CourseSelectionPage');
const EnrollmentStatusPage = loadPage(() => import('./pages/EnrollmentStatusPage'), 'EnrollmentStatusPage');
const CourseHistoryPage = loadPage(() => import('./pages/CourseHistoryPage'), 'CourseHistoryPage');
const NotFound = loadPage(() => import('./pages/NotFound'), 'NotFound');

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000,
    },
  },
});

function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <CustomThemeProvider>
          <ThemeProvider theme={theme}>
            <CssBaseline />
            <SnackbarProvider
              maxSnack={3}
              autoHideDuration={6000}
              anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
            >
              <BrowserRouter>
                <AuthProvider>
                  <Suspense
                    fallback={
                      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                        Loading...
                      </div>
                    }
                  >
                    <Routes>
                      <Route path="/login" element={<Login />} />
                      <Route path="/register" element={<Register />} />
                      <Route path="/verify" element={<CertificateVerification />} />
                      <Route path="/" element={<ProtectedRoute><Layout /></ProtectedRoute>}>
                        <Route index element={<Navigate to="/dashboard" />} />
                        <Route path="dashboard" element={<Dashboard />} />
                        <Route path="students" element={<Students />} />
                        <Route path="students/:id" element={<StudentDetail />} />
                        <Route path="lecturers" element={<Lecturers />} />
                        <Route path="courses" element={<Courses />} />
                        <Route path="course-offerings" element={<CourseOfferings />} />
                        <Route path="course-offerings/new" element={<CourseOfferingFormPage />} />
                        <Route path="course-offerings/:id" element={<CourseOfferingDetail />} />
                        <Route path="course-offerings/:id/edit" element={<CourseOfferingFormPage />} />
                        <Route path="units" element={<Units />} />
                        <Route path="timetable" element={<Timetable />} />
                        <Route path="accommodation" element={<Accommodation />} />
                        <Route path="assignments" element={<Assignments />} />
                        <Route path="grades" element={<Grades />} />
                        <Route path="reports" element={<Reports />} />
                        <Route path="users" element={<Users />} />
                        <Route path="settings" element={<Settings />} />
                        <Route path="profile" element={<Profile />} />
                        <Route path="calendar" element={<Calendar />} />
                        <Route path="certificates" element={<Certificates />} />
                        <Route path="certificates/templates" element={<CertificateTemplates />} />
                        <Route path="my-certificates" element={<MyCertificates />} />
                        <Route path="course-selection" element={<CourseSelectionPage />} />
                        <Route path="enrollment-status" element={<EnrollmentStatusPage />} />
                        <Route path="course-history" element={<CourseHistoryPage />} />
                        <Route path="*" element={<NotFound />} />
                      </Route>
                    </Routes>
                  </Suspense>
                </AuthProvider>
              </BrowserRouter>
            </SnackbarProvider>
          </ThemeProvider>
        </CustomThemeProvider>
        <ReactQueryDevtools initialIsOpen={false} />
      </QueryClientProvider>
    </ErrorBoundary>
  );
}

export default App;

