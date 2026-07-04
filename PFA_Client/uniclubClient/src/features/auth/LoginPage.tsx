import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import type { TokenResponse, UserClub, PagedResult } from '../../types';

export default function LoginPage() {
  const navigate = useNavigate();
  const { setTokens, setAdminClubIds, hasRole } = useAuthStore();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const { data } = await apiClient.post<TokenResponse>(
        '/identity/api/account/login',
        { email, password }
      );

      setTokens(data.accessToken, data.refreshToken);

      // Compute admin club IDs for club-scoped checks
      if (hasRole('ClubMember')) {
        try {
          const clubsRes = await apiClient.get<PagedResult<UserClub>>(
            '/clubs/api/clubs/user-clubs',
            { params: { pageSize: 100 } }
          );
          const adminIds = clubsRes.data.items
            .filter((c) => c.userPost && c.userPost !== 'Member')
            .map((c) => c.id);
          setAdminClubIds(adminIds);
        } catch {
          // non-fatal — user still logs in
        }
      }

      navigate('/clubs');
    } catch {
      setError('Invalid email or password.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow-sm" style={{ width: '100%', maxWidth: '420px' }}>
        <div className="card-body p-4">
          <h4 className="card-title text-center mb-1 fw-bold text-primary">
            UniClub
          </h4>
          <p className="text-center text-muted mb-4 small">
            Sign in to your account
          </p>

          {error && (
            <div className="alert alert-danger py-2 small">{error}</div>
          )}

          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label className="form-label">Email</label>
              <input
                type="email"
                className="form-control"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
              />
            </div>
            <div className="mb-4">
              <label className="form-label">Password</label>
              <input
                type="password"
                className="form-control"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <button
              type="submit"
              className="btn btn-primary w-100"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" />
                  Signing in…
                </>
              ) : (
                'Sign In'
              )}
            </button>
          </form>

          <p className="text-center mt-3 mb-0 small text-muted">
            Don&apos;t have an account?{' '}
            <Link to="/register">Register</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
