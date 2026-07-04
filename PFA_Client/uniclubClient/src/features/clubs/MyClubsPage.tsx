import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import Pagination from '../../components/Pagination';
import type { UserClub, PagedResult } from '../../types';

const PAGE_SIZE = 9;

export default function MyClubsPage() {
  const { isAdmin, setAdminClubIds } = useAuthStore();
  const [clubs, setClubs] = useState<UserClub[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const fetchClubs = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<UserClub>>(
        '/clubs/api/clubs/user-clubs',
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setClubs(data.items);
      setTotalCount(data.totalCount);

      // Keep adminClubIds up to date
      const adminIds = data.items
        .filter((c) => c.userPost && c.userPost !== 'Member')
        .map((c) => c.id);
      setAdminClubIds(adminIds);
    } finally {
      setLoading(false);
    }
  }, [page, setAdminClubIds]);

  useEffect(() => { fetchClubs(); }, [fetchClubs]);

  if (loading) {
    return (
      <div className="text-center py-5">
        <span className="spinner-border text-primary" />
      </div>
    );
  }

  return (
    <div>
      <h2 className="fw-bold mb-4">My Clubs</h2>

      {clubs.length === 0 ? (
        <div className="text-center text-muted py-5">
          You are not a member of any club yet.{' '}
          <Link to="/clubs">Browse clubs</Link> to join one.
        </div>
      ) : (
        <div className="row g-3">
          {clubs.map((club) => (
            <div key={club.id} className="col-12 col-md-6 col-lg-4">
              <div className="card h-100 shadow-sm border-0">
                <div className="card-body d-flex flex-column">
                  <div className="d-flex justify-content-between align-items-start mb-2">
                    <h5 className="card-title fw-semibold mb-0">{club.name}</h5>
                    <span className="badge bg-primary ms-2">
                      {club.userPost ?? 'Member'}
                    </span>
                  </div>
                  <p className="card-text text-muted small flex-grow-1">
                    {club.description}
                  </p>

                  <div className="d-flex flex-wrap gap-2 mt-auto">
                    <Link
                      to={`/clubs/${club.id}`}
                      className="btn btn-outline-primary btn-sm"
                    >
                      View
                    </Link>

                    {isAdmin(club.id) && (
                      <>
                        <Link
                          to={`/clubs/${club.id}/members`}
                          className="btn btn-outline-secondary btn-sm"
                        >
                          Members
                        </Link>
                        <Link
                          to={`/clubs/${club.id}/adhesions`}
                          className="btn btn-outline-secondary btn-sm"
                        >
                          Adhesions
                        </Link>
                        <Link
                          to={`/clubs/${club.id}/announcements`}
                          className="btn btn-outline-secondary btn-sm"
                        >
                          Announcements
                        </Link>
                        <Link
                          to={`/clubs/${club.id}/events/create`}
                          className="btn btn-primary btn-sm"
                        >
                          + Event
                        </Link>
                      </>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="mt-4">
        <Pagination
          currentPage={page}
          totalCount={totalCount}
          pageSize={PAGE_SIZE}
          onPageChange={setPage}
        />
      </div>
    </div>
  );
}
