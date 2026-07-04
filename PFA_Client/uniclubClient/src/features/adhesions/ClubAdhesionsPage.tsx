import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import { useToast } from '../../components/ToastProvider';
import Pagination from '../../components/Pagination';
import type { Adhesion, PagedResult } from '../../types';

const PAGE_SIZE = 10;

const statusClass: Record<string, string> = {
  Pending: 'bg-warning text-dark',
  Accepted: 'bg-success',
  Refused: 'bg-danger',
};

export default function ClubAdhesionsPage() {
  const { clubId } = useParams<{ clubId: string }>();
  const id = Number(clubId);
  const { isAdmin } = useAuthStore();
  const { showToast } = useToast();

  const [adhesions, setAdhesions] = useState<Adhesion[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [actionId, setActionId] = useState<number | null>(null);

  const fetchAdhesions = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<Adhesion>>(
        `/clubs/api/adhesions/club/${id}`,
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setAdhesions(data.items);
      setTotalCount(data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [id, page]);

  useEffect(() => { fetchAdhesions(); }, [fetchAdhesions]);

  if (!isAdmin(id)) {
    return (
      <div className="alert alert-danger">
        You do not have admin access to this club.
      </div>
    );
  }

  async function handleAction(
    adhesionId: number,
    action: 'accept' | 'refuse' | 'delete'
  ) {
    if (action === 'delete' && !confirm('Delete this request?')) return;
    setActionId(adhesionId);
    try {
      if (action === 'delete') {
        await apiClient.delete(`/clubs/api/adhesions/${adhesionId}`);
      } else {
        await apiClient.put(`/clubs/api/adhesions/${adhesionId}/${action}`);
      }
      showToast(
        action === 'accept'
          ? 'Request accepted.'
          : action === 'refuse'
          ? 'Request refused.'
          : 'Request deleted.',
        'success'
      );
      fetchAdhesions();
    } catch {
      showToast('Action failed. Please try again.', 'danger');
    } finally {
      setActionId(null);
    }
  }

  return (
    <div>
      <h2 className="fw-bold mb-4">Membership Requests</h2>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : adhesions.length === 0 ? (
        <div className="text-center text-muted py-5">No requests.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {adhesions.map((a) => (
                <tr key={a.id}>
                  <td>
                    {a.user.firstName} {a.user.lastName}
                  </td>
                  <td>{a.user.email}</td>
                  <td>
                    <span className={`badge ${statusClass[a.status]}`}>
                      {a.status}
                    </span>
                  </td>
                  <td>
                    <div className="d-flex gap-1 flex-wrap">
                      {a.status === 'Pending' && (
                        <>
                          <button
                            className="btn btn-success btn-sm"
                            disabled={actionId === a.id}
                            onClick={() => handleAction(a.id, 'accept')}
                          >
                            Accept
                          </button>
                          <button
                            className="btn btn-warning btn-sm"
                            disabled={actionId === a.id}
                            onClick={() => handleAction(a.id, 'refuse')}
                          >
                            Refuse
                          </button>
                        </>
                      )}
                      <button
                        className="btn btn-outline-danger btn-sm"
                        disabled={actionId === a.id}
                        onClick={() => handleAction(a.id, 'delete')}
                      >
                        Delete
                      </button>
                    </div>
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
