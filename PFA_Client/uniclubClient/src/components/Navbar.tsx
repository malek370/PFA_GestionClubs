import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';

export default function Navbar() {
  const { accessToken, userEmail, hasRole, logout } = useAuthStore();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
      <div className="container">
        <Link className="navbar-brand fw-bold" to="/">
          UniClub
        </Link>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarMain"
          aria-controls="navbarMain"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon" />
        </button>

        <div className="collapse navbar-collapse" id="navbarMain">
          {accessToken ? (
            <>
              <ul className="navbar-nav me-auto mb-2 mb-lg-0">
                <li className="nav-item">
                  <NavLink className="nav-link" to="/clubs">
                    Clubs
                  </NavLink>
                </li>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/events">
                    Events
                  </NavLink>
                </li>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/my-events">
                    My Events
                  </NavLink>
                </li>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/my-adhesions">
                    My Requests
                  </NavLink>
                </li>
                {hasRole('ClubMember') && (
                  <li className="nav-item">
                    <NavLink className="nav-link" to="/my-clubs">
                      My Clubs
                    </NavLink>
                  </li>
                )}
                {hasRole('PlatformAdmin') && (
                  <li className="nav-item">
                    <NavLink className="nav-link" to="/clubs/create">
                      Create Club
                    </NavLink>
                  </li>
                )}
              </ul>

              <ul className="navbar-nav ms-auto mb-2 mb-lg-0 align-items-lg-center">
                <li className="nav-item">
                  <span className="nav-link text-white-50 small">{userEmail}</span>
                </li>
                <li className="nav-item">
                  <button
                    className="btn btn-outline-light btn-sm"
                    onClick={handleLogout}
                  >
                    Logout
                  </button>
                </li>
              </ul>
            </>
          ) : (
            <ul className="navbar-nav ms-auto mb-2 mb-lg-0">
              <li className="nav-item">
                <NavLink className="nav-link" to="/login">
                  Login
                </NavLink>
              </li>
              <li className="nav-item">
                <NavLink className="nav-link" to="/register">
                  Register
                </NavLink>
              </li>
            </ul>
          )}
        </div>
      </div>
    </nav>
  );
}
