import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import type { Club, ClubEvent, Adhesion, PagedResult } from '../../types';
import Pagination from '../../components/Pagination';
import RoleGuard from '../../components/RoleGuard';

const PAGE_SIZE = 5;

export default function ClubDetailPage() {
  const { clubId } = useParams<{ clubId: string }>();
  const id = Number(clubId);
  const { isAdmin } = useAuthStore();

  const [club, setClub] = useState<Club | null>(null);
  const [events, setEvents] = useState<ClubEvent[]>([]);
  const [eventsTotal, setEventsTotal] = useState(0);
  const [eventsPage, setEventsPage] = useState(1);
  const [myAdhesions, setMyAdhesions] = useState<Adhesion[]>([]);
  const [tab, setTab] = useState<'events' | 'announcements'>('events');
  const [loadingClub, setLoadingClub] = useState(true);
  const [joinLoading, setJoinLoading] = useState(false);
  const [joinError, setJoinError] = useState('');

  // Fetch club info (from list endpoint — we match by id)
  useEffect(() => {
    apiClient
      .get<PagedResult<Club>>('/clubs/api/clubs', { params: { pageSize: 200 } })
      .then(({ data }) => {
        const found = data.items.find((c) => c.id === id) ?? null;
        setClub(found);
      })
      .finally(() => setLoadingClub(false));
  }, [id]);

  // Fetch club events
  useEffect(() => {
    apiClient
      .get<PagedResult<ClubEvent>>(
        `/clubs/api/events/club-events/${id}`,
        { params: { pageNumber: eventsPage, pageSize: PAGE_SIZE } }
      )
      .then(({ data }) => {
        setEvents(data.items);
        setEventsTotal(data.totalCount);
      });
  }, [id, eventsPage]);

  // Fetch my adhesions for Join button logic
  useEffect(() => {
    apiClient
      .get<PagedResult<Adhesion>>('/clubs/api/adhesions/myadhesions', {
        params: { pageSize: 200 },
      })
      .then(({ data }) => setMyAdhesions(data.items))
      .catch(() => setMyAdhesions([]));
  }, [id]);

  const myAdhesion = myAdhesions.find((a) => a.clubName === club?.name);
  const canJoin = !myAdhesion && !isAdmin(id);

  async function handleJoin() {
    setJoinError('');
    setJoinLoading(true);
    try {
      await apiClient.post('/clubs/api/adhesions', { clubId: id });
      // Re-fetch adhesions to reflect new state
      const { data } = await apiClient.get<PagedResult<Adhesion>>(
        '/clubs/api/adhesions/myadhesions',
        { params: { pageSize: 200 } }
      );
      setMyAdhesions(data.items);
    } catch {
      setJoinError('Could not submit request. Please try again.');
    } finally {
      setJoinLoading(false);
    }
  }

  if (loadingClub) {
    return (
      <div className="text-center py-5">
        <span className="spinner-border text-primary" />
      </div>
    );
  }

  if (!club) {
    return <div className="alert alert-warning">Club not found.</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="d-flex align-items-start justify-content-between mb-4 flex-wrap gap-2">
        <div>
          <h2 className="fw-bold mb-1">{club.name}</h2>
          <p className="text-muted mb-1">{club.description}</p>
          {club.presidentMail && (
            <span className="badge bg-secondary">
              President: {club.presidentMail}
            </span>
          )}
        </div>

        <div className="d-flex flex-wrap gap-2 align-items-start">
          {/* Join button */}
          {canJoin && (
            <button
              className="btn btn-success btn-sm"
              onClick={handleJoin}
              disabled={joinLoading}
            >
              {joinLoading ? (
                <span className="spinner-border spinner-border-sm" />
              ) : (
                'Join Club'
              )}
            </button>
          )}
          {myAdhesion && (
            <span
              className={`badge fs-6 ${
                myAdhesion.status === 'Accepted'
                  ? 'bg-success'
                  : myAdhesion.status === 'Refused'
                  ? 'bg-danger'
                  : 'bg-warning text-dark'
              }`}
            >
              {myAdhesion.status}
            </span>
          )}

          {/* Admin controls */}
          <RoleGuard clubId={id}>
            <Link
              to={`/clubs/${id}/members`}
              className="btn btn-outline-secondary btn-sm"
            >
              Members
            </Link>
            <Link
              to={`/clubs/${id}/adhesions`}
              className="btn btn-outline-secondary btn-sm"
            >
              Adhesions
            </Link>
            <Link
              to={`/clubs/${id}/announcements`}
              className="btn btn-outline-secondary btn-sm"
            >
              Announcements
            </Link>
            <Link
              to={`/clubs/${id}/events/create`}
              className="btn btn-primary btn-sm"
            >
              + Event
            </Link>
          </RoleGuard>
        </div>
      </div>

      {joinError && (
        <div className="alert alert-danger py-2 small">{joinError}</div>
      )}

      {/* Tabs */}
      <ul className="nav nav-tabs mb-3">
        <li className="nav-item">
          <button
            className={`nav-link ${tab === 'events' ? 'active' : ''}`}
            onClick={() => setTab('events')}
          >
            Events
          </button>
        </li>
        {isAdmin(id) && (
          <li className="nav-item">
            <button
              className={`nav-link ${tab === 'announcements' ? 'active' : ''}`}
              onClick={() => setTab('announcements')}
            >
              Announcements
            </button>
          </li>
        )}
      </ul>

      {tab === 'events' && (
        <div>
          {events.length === 0 ? (
            <p className="text-muted">No events yet.</p>
          ) : (
            <div className="list-group mb-3">
              {events.map((ev) => (
                <Link
                  key={ev.id}
                  to={`/events/${ev.id}`}
                  className="list-group-item list-group-item-action"
                >
                  <div className="d-flex justify-content-between">
                    <span className="fw-semibold">{ev.title}</span>
                    <small className="text-muted">
                      {new Date(ev.startDate).toLocaleDateString()}
                    </small>
                  </div>
                  {ev.location && (
                    <small className="text-muted">{ev.location}</small>
                  )}
                </Link>
              ))}
            </div>
          )}
          <Pagination
            currentPage={eventsPage}
            totalCount={eventsTotal}
            pageSize={PAGE_SIZE}
            onPageChange={setEventsPage}
          />
        </div>
      )}

      {tab === 'announcements' && isAdmin(id) && (
        <div>
          <p className="text-muted mb-3 small">
            Manage all announcements for this club from the dedicated page.
          </p>
          <Link
            to={`/clubs/${id}/announcements`}
            className="btn btn-primary"
          >
            Open Announcements Manager →
          </Link>
        </div>
      )}
    </div>
  );
}
