import { Outlet } from 'react-router-dom';
import Navbar from './Navbar';
import { lazy, Suspense, useEffect } from 'react';
import { useAuthStore } from '../stores/authStore';
import apiClient from '../lib/apiClient';
import type { PagedResult, UserClub } from '../types';

const ChatbotWidget = lazy(
  () => import('../features/chatbot/ChatbotWidget')
);

export default function Layout() {
  const { accessToken, hasRole, setAdminClubIds } = useAuthStore();

  // Re-sync adminClubIds from the server on every fresh session mount
  useEffect(() => {
    if (!accessToken || !hasRole('ClubMember')) return;
    apiClient
      .get<PagedResult<UserClub>>('/clubs/api/clubs/user-clubs', {
        params: { pageSize: 100 },
      })
      .then(({ data }) => {
        const adminIds = data.items
          .filter((c) => c.userPost && c.userPost !== 'Member')
          .map((c) => c.id);
        setAdminClubIds(adminIds);
      })
      .catch(() => {});
    // run once per session (when accessToken changes)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);

  return (
    <>
      <Navbar />
      <main className="container py-4">
        <Outlet />
      </main>
      <Suspense fallback={null}>
        <ChatbotWidget />
      </Suspense>
    </>
  );
}
