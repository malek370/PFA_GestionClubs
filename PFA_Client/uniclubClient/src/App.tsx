import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ToastProvider } from './components/ToastProvider';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import { useAuthStore } from './stores/authStore';

// Auth
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';

// Clubs
import ClubListPage from './features/clubs/ClubListPage';
import ClubDetailPage from './features/clubs/ClubDetailPage';
import MyClubsPage from './features/clubs/MyClubsPage';
import CreateClubForm from './features/clubs/CreateClubForm';

// Events
import EventListPage from './features/events/EventListPage';
import EventDetailPage from './features/events/EventDetailPage';
import MyEventsPage from './features/events/MyEventsPage';
import CreateEventForm from './features/events/CreateEventForm';

// Adhesions
import MyAdhesionsPage from './features/adhesions/MyAdhesionsPage';
import ClubAdhesionsPage from './features/adhesions/ClubAdhesionsPage';

// Members
import ClubMembersPage from './features/members/ClubMembersPage';

// Announcements
import ClubAnnouncementsPage from './features/announcements/ClubAnnouncementsPage';
import AnnouncementDetail from './features/announcements/AnnouncementDetail';

// 404
import NotFoundPage from './features/NotFoundPage';

function RootRedirect() {
  const { accessToken } = useAuthStore();
  return <Navigate to={accessToken ? '/clubs' : '/login'} replace />;
}

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
          {/* Public routes */}
          <Route path="/" element={<RootRedirect />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected routes — wrapped in Layout (Navbar + ChatbotWidget) */}
          <Route
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            {/* Clubs */}
            <Route path="/clubs" element={<ClubListPage />} />
            <Route
              path="/clubs/create"
              element={
                <ProtectedRoute requiredRole="PlatformAdmin">
                  <CreateClubForm />
                </ProtectedRoute>
              }
            />
            <Route path="/clubs/:clubId" element={<ClubDetailPage />} />
            <Route
              path="/clubs/:clubId/members"
              element={<ClubMembersPage />}
            />
            <Route
              path="/clubs/:clubId/adhesions"
              element={<ClubAdhesionsPage />}
            />
            <Route
              path="/clubs/:clubId/announcements"
              element={<ClubAnnouncementsPage />}
            />
            <Route
              path="/clubs/:clubId/events/create"
              element={<CreateEventForm />}
            />
            <Route
              path="/my-clubs"
              element={
                <ProtectedRoute requiredRole="ClubMember">
                  <MyClubsPage />
                </ProtectedRoute>
              }
            />

            {/* Events */}
            <Route path="/events" element={<EventListPage />} />
            <Route path="/events/:id" element={<EventDetailPage />} />
            <Route path="/my-events" element={<MyEventsPage />} />

            {/* Adhesions */}
            <Route path="/my-adhesions" element={<MyAdhesionsPage />} />

            {/* Announcements */}
            <Route
              path="/announcements/:id"
              element={
                <ProtectedRoute requiredRole="ClubMember">
                  <AnnouncementDetail />
                </ProtectedRoute>
              }
            />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  );
}
