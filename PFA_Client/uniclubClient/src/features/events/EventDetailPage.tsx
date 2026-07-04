import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import { useToast } from '../../components/ToastProvider';
import type { ClubEvent, PagedResult } from '../../types';

export default function EventDetailPage() {
  const { id } = useParams<{ id: string }>();
  const eventId = Number(id);
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [event, setEvent] = useState<ClubEvent | null>(null);
  const [joined, setJoined] = useState(false);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    Promise.all([
      apiClient.get<ClubEvent>(`/clubs/api/events/${eventId}`),
      apiClient.get<PagedResult<ClubEvent>>('/clubs/api/events/user-events', {
        params: { pageSize: 200 },
      }),
    ]).then(([evRes, myRes]) => {
      setEvent(evRes.data);
      setJoined(myRes.data.items.some((e) => e.id === eventId));
    }).finally(() => setLoading(false));
  }, [eventId]);

  async function handleJoin() {
    setActionLoading(true);
    try {
      await apiClient.put(`/clubs/api/events/${eventId}/join`);
      setJoined(true);
      showToast('Joined event!', 'success');
    } catch {
      showToast('Could not join event.', 'danger');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleLeave() {
    setActionLoading(true);
    try {
      await apiClient.put(`/clubs/api/events/${eventId}/leave`);
      setJoined(false);
      showToast('Left event.', 'info');
    } catch {
      showToast('Could not leave event.', 'danger');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleDelete() {
    if (!confirm('Delete this event? This cannot be undone.')) return;
    try {
      await apiClient.delete(`/clubs/api/events/${eventId}`);
      showToast('Event deleted.', 'success');
      navigate('/events');
    } catch {
      showToast('Could not delete event.', 'danger');
    }
  }

  if (loading) {
    return (
      <div className="text-center py-5">
        <span className="spinner-border text-primary" />
      </div>
    );
  }

  if (!event) {
    return <div className="alert alert-warning">Event not found.</div>;
  }

  return (
    <div className="row justify-content-center">
      <div className="col-12 col-lg-8">
        <Link to="/events" className="text-muted small mb-3 d-block">
          ← Back to Events
        </Link>

        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
              <h2 className="fw-bold mb-0">{event.title}</h2>
              <span className="badge bg-secondary">{event.clubName}</span>
            </div>

            <p className="text-muted">{event.description}</p>

            <div className="row g-2 mb-4">
              <div className="col-auto">
                <span className="badge bg-light text-dark border">
                  📅 {new Date(event.startDate).toLocaleString()}
                </span>
              </div>
              {event.location && (
                <div className="col-auto">
                  <span className="badge bg-light text-dark border">
                    📍 {event.location}
                  </span>
                </div>
              )}
            </div>

            <div className="d-flex gap-2 flex-wrap">
              {joined ? (
                <button
                  className="btn btn-outline-danger"
                  onClick={handleLeave}
                  disabled={actionLoading}
                >
                  {actionLoading ? (
                    <span className="spinner-border spinner-border-sm me-2" />
                  ) : null}
                  Leave Event
                </button>
              ) : (
                <button
                  className="btn btn-success"
                  onClick={handleJoin}
                  disabled={actionLoading}
                >
                  {actionLoading ? (
                    <span className="spinner-border spinner-border-sm me-2" />
                  ) : null}
                  Join Event
                </button>
              )}

              {/* Find the club id by matching name — delete only for admin */}
              <RoleGuardDelete
                clubName={event.clubName}
                onDelete={handleDelete}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// Small inline helper: finds the club matching this event's clubName from adminClubIds
function RoleGuardDelete({
  clubName,
  onDelete,
}: {
  clubName: string;
  onDelete: () => void;
}) {
  const { adminClubIds } = useAuthStore();
  const [clubs, setClubs] = useState<{ id: number; name: string }[]>([]);

  useEffect(() => {
    if (adminClubIds.length === 0) return;
    apiClient
      .get<{ items: { id: number; name: string }[] }>('/clubs/api/clubs', {
        params: { pageSize: 200 },
      })
      .then(({ data }) => setClubs(data.items))
      .catch(() => {});
  }, [adminClubIds]);

  const isAdminOfThisClub = clubs.some(
    (c) => c.name === clubName && adminClubIds.includes(c.id)
  );

  if (!isAdminOfThisClub) return null;

  return (
    <button className="btn btn-outline-danger" onClick={onDelete}>
      Delete Event
    </button>
  );
}
