import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import { useToast } from '../../components/ToastProvider';
import Pagination from '../../components/Pagination';
import type { Announcement, PagedResult } from '../../types';

const PAGE_SIZE = 10;

export default function ClubAnnouncementsPage() {
  const { clubId } = useParams<{ clubId: string }>();
  const id = Number(clubId);
  const { isAdmin } = useAuthStore();
  const { showToast } = useToast();

  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  // Create form
  const [form, setForm] = useState({ title: '', content: '', isPublic: false });
  const [creating, setCreating] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const fetchAnnouncements = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<Announcement>>(
        `/clubs/api/annoucements/club/${id}`,
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setAnnouncements(data.items);
      setTotalCount(data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [id, page]);

  useEffect(() => { fetchAnnouncements(); }, [fetchAnnouncements]);

  if (!isAdmin(id)) {
    return (
      <div className="alert alert-danger">
        You do not have admin access to this club.
      </div>
    );
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true);
    try {
      await apiClient.post('/clubs/api/annoucements', {
        title: form.title,
        content: form.content,
        clubId: id,
        isPublic: form.isPublic,
      });
      showToast('Announcement created.', 'success');
      setForm({ title: '', content: '', isPublic: false });
      setShowForm(false);
      fetchAnnouncements();
    } catch {
      showToast('Failed to create announcement.', 'danger');
    } finally {
      setCreating(false);
    }
  }

  async function handleDelete(announcementId: number) {
    if (!confirm('Delete this announcement?')) return;
    setDeletingId(announcementId);
    try {
      await apiClient.delete(`/clubs/api/annoucements/${announcementId}`);
      showToast('Announcement deleted.', 'success');
      fetchAnnouncements();
    } catch {
      showToast('Failed to delete announcement.', 'danger');
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Announcements</h2>
        <button
          className="btn btn-primary btn-sm"
          onClick={() => setShowForm((v) => !v)}
        >
          {showForm ? 'Cancel' : '+ New Announcement'}
        </button>
      </div>

      {showForm && (
        <div className="card border-0 shadow-sm mb-4">
          <div className="card-body">
            <form onSubmit={handleCreate}>
              <div className="mb-3">
                <label className="form-label">Title</label>
                <input
                  type="text"
                  className="form-control"
                  value={form.title}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, title: e.target.value }))
                  }
                  required
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Content</label>
                <textarea
                  className="form-control"
                  rows={4}
                  value={form.content}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, content: e.target.value }))
                  }
                  required
                />
              </div>
              <div className="form-check mb-3">
                <input
                  id="isPublic"
                  type="checkbox"
                  className="form-check-input"
                  checked={form.isPublic}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, isPublic: e.target.checked }))
                  }
                />
                <label htmlFor="isPublic" className="form-check-label">
                  Public
                </label>
              </div>
              <button type="submit" className="btn btn-primary" disabled={creating}>
                {creating && (
                  <span className="spinner-border spinner-border-sm me-2" />
                )}
                Create
              </button>
            </form>
          </div>
        </div>
      )}

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : announcements.length === 0 ? (
        <div className="text-center text-muted py-5">No announcements yet.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-light">
              <tr>
                <th>Title</th>
                <th>Club</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {announcements.map((a) => (
                <tr key={a.id}>
                  <td className="fw-semibold">{a.title}</td>
                  <td>{a.clubName}</td>
                  <td>
                    <div className="d-flex gap-2">
                      <Link
                        to={`/announcements/${a.id}`}
                        className="btn btn-outline-primary btn-sm"
                      >
                        View
                      </Link>
                      <button
                        className="btn btn-outline-danger btn-sm"
                        disabled={deletingId === a.id}
                        onClick={() => handleDelete(a.id)}
                      >
                        {deletingId === a.id ? (
                          <span className="spinner-border spinner-border-sm" />
                        ) : (
                          'Delete'
                        )}
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
