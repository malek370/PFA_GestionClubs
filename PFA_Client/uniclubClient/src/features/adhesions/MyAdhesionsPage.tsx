import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import Pagination from '../../components/Pagination';
import type { Adhesion, PagedResult } from '../../types';

const PAGE_SIZE = 10;

const statusClass: Record<string, string> = {
  Pending: 'bg-warning text-dark',
  Accepted: 'bg-success',
  Refused: 'bg-danger',
};

export default function MyAdhesionsPage() {
  const [adhesions, setAdhesions] = useState<Adhesion[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const fetchAdhesions = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<Adhesion>>(
        '/clubs/api/adhesions/myadhesions',
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setAdhesions(data.items);
      setTotalCount(data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => { fetchAdhesions(); }, [fetchAdhesions]);

  return (
    <div>
      <h2 className="fw-bold mb-4">My Membership Requests</h2>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : adhesions.length === 0 ? (
        <div className="text-center text-muted py-5">
          No membership requests yet.{' '}
          <Link to="/clubs">Browse clubs</Link> to join one.
        </div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-light">
              <tr>
                <th>Club</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {adhesions.map((a) => (
                <tr key={a.id}>
                  <td>{a.clubName}</td>
                  <td>
                    <span className={`badge ${statusClass[a.status]}`}>
                      {a.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        currentPage={page}
        totalCount={totalCount}
        pageSize={PAGE_SIZE}
        onPageChange={setPage}
      />
    </div>
  );
}
