import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import { useToast } from '../../components/ToastProvider';
import Pagination from '../../components/Pagination';
import type { Member, PostInClub, PagedResult } from '../../types';

const PAGE_SIZE = 10;

const POSTS: PostInClub[] = [
  'Member',
  'HeadOfDepartment',
  'President',
  'Secretary',
  'Treasurer',
  'VicePresident',
];

const postColor: Record<PostInClub, string> = {
  President: 'bg-danger',
  VicePresident: 'bg-warning text-dark',
  Treasurer: 'bg-info text-dark',
  Secretary: 'bg-primary',
  HeadOfDepartment: 'bg-secondary',
  Member: 'bg-light text-dark border',
};

export default function ClubMembersPage() {
  const { clubId } = useParams<{ clubId: string }>();
  const id = Number(clubId);
  const { isAdmin } = useAuthStore();
  const { showToast } = useToast();

  const [members, setMembers] = useState<Member[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [postSelections, setPostSelections] = useState<Record<number, PostInClub>>({});
  const [savingId, setSavingId] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const fetchMembers = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<Member>>(
        `/clubs/api/members/club/${id}`,
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setMembers(data.items);
      setTotalCount(data.totalCount);
      // Initialize post selections
      const initial: Record<number, PostInClub> = {};
      data.items.forEach((m) => { initial[m.id] = m.postInClub; });
      setPostSelections(initial);
    } finally {
      setLoading(false);
    }
  }, [id, page]);

  useEffect(() => { fetchMembers(); }, [fetchMembers]);

  if (!isAdmin(id)) {
    return (
      <div className="alert alert-danger">
        You do not have admin access to this club.
      </div>
    );
  }

  async function handleChangePost(memberId: number) {
    const newPost = postSelections[memberId];
    setSavingId(memberId);
    try {
      await apiClient.put('/clubs/api/members/post', { memberId, newPost });
      showToast('Post updated.', 'success');
      fetchMembers();
    } catch {
      showToast('Failed to update post.', 'danger');
    } finally {
      setSavingId(null);
    }
  }

  async function handleRemove(memberId: number) {
    if (!confirm('Remove this member from the club? This cannot be undone.')) return;
    setDeletingId(memberId);
    try {
      await apiClient.delete(`/clubs/api/members/${memberId}`);
      showToast('Member removed.', 'success');
      fetchMembers();
    } catch {
      showToast('Failed to remove member.', 'danger');
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div>
      <h2 className="fw-bold mb-4">Club Members</h2>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : members.length === 0 ? (
        <div className="text-center text-muted py-5">No members yet.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Current Post</th>
                <th>Change Post</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {members.map((m) => (
                <tr key={m.id}>
                  <td>
                    {m.user.firstName} {m.user.lastName}
                  </td>
                  <td className="text-muted small">{m.user.email}</td>
                  <td>
                    <span className={`badge ${postColor[m.postInClub]}`}>
                      {m.postInClub}
                    </span>
                  </td>
                  <td>
                    <div className="d-flex gap-2 align-items-center">
                      <select
                        className="form-select form-select-sm"
                        style={{ width: '160px' }}
                        value={postSelections[m.id] ?? m.postInClub}
                        onChange={(e) =>
                          setPostSelections((prev) => ({
                            ...prev,
                            [m.id]: e.target.value as PostInClub,
                          }))
                        }
                      >
                        {POSTS.map((p) => (
                          <option key={p} value={p}>
                            {p}
                          </option>
                        ))}
                      </select>
                      <button
                        className="btn btn-sm btn-outline-primary"
                        disabled={
                          savingId === m.id ||
                          postSelections[m.id] === m.postInClub
                        }
                        onClick={() => handleChangePost(m.id)}
                      >
                        {savingId === m.id ? (
                          <span className="spinner-border spinner-border-sm" />
                        ) : (
                          'Save'
                        )}
                      </button>
                    </div>
                    {postSelections[m.id] === 'President' &&
                      postSelections[m.id] !== m.postInClub && (
                        <div className="alert alert-warning py-1 px-2 mt-1 small mb-0">
                          ⚠️ Promoting to President grants ClubAdmin role. The
                          user must re-login to apply it.
                        </div>
                      )}
                  </td>
                  <td>
                    <button
                      className="btn btn-outline-danger btn-sm"
                      disabled={deletingId === m.id}
                      onClick={() => handleRemove(m.id)}
                    >
                      {deletingId === m.id ? (
                        <span className="spinner-border spinner-border-sm" />
                      ) : (
                        'Remove'
                      )}
                    </button>
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
