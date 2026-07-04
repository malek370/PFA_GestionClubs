import { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import Pagination from '../../components/Pagination';
import type { Club, PagedResult } from '../../types';

const PAGE_SIZE = 9;

export default function ClubListPage() {
  const navigate = useNavigate();
  const { hasRole } = useAuthStore();

  const [clubs, setClubs] = useState<Club[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [nameFilter, setNameFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');

  const fetchClubs = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const { data } = await apiClient.get<PagedResult<Club>>(
        '/clubs/api/clubs',
        { params: { name: search || undefined, pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setClubs(data.items);
      setTotalCount(data.totalCount);
    } catch {
      setError('Failed to load clubs. Make sure the backend is running at http://localhost:5000.');
    } finally {
      setLoading(false);
    }
  }, [search, page]);

  useEffect(() => { fetchClubs(); }, [fetchClubs]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setSearch(nameFilter);
    setPage(1);
  }

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-4">
        <h2 className="fw-bold mb-0">Clubs</h2>
        {hasRole('PlatformAdmin') && (
          <button
            className="btn btn-primary btn-sm"
            onClick={() => navigate('/clubs/create')}
          >
            + Create Club
          </button>
        )}
      </div>

      <form onSubmit={handleSearch} className="d-flex gap-2 mb-4">
        <input
          type="text"
          className="form-control"
          placeholder="Search clubs…"
          value={nameFilter}
          onChange={(e) => setNameFilter(e.target.value)}
        />
        <button type="submit" className="btn btn-outline-primary px-4">
          Search
        </button>
      </form>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : error ? (
        <div className="alert alert-danger">{error}</div>
      ) : clubs.length === 0 ? (
        <div className="text-center text-muted py-5">No clubs found.</div>
      ) : (
        <div className="row g-3">
          {clubs.map((club) => (
            <div key={club.id} className="col-12 col-md-6 col-lg-4">
              <div className="card h-100 shadow-sm border-0">
                <div className="card-body d-flex flex-column">
                  <h5 className="card-title fw-semibold">{club.name}</h5>
                  <p className="card-text text-muted small flex-grow-1">
                    {club.description}
                  </p>
                  {club.presidentMail && (
                    <p className="card-text small text-muted mb-2">
                      <span className="fw-semibold">President:</span>{' '}
                      {club.presidentMail}
                    </p>
                  )}
                  <Link
                    to={`/clubs/${club.id}`}
                    className="btn btn-outline-primary btn-sm mt-auto"
                  >
                    View
                  </Link>
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
