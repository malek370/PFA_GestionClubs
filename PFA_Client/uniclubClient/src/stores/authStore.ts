import { create } from 'zustand';
import { getRoles, getEmail } from '../lib/auth';

const ACCESS_TOKEN_KEY = 'uc_access_token';
const REFRESH_TOKEN_KEY = 'uc_refresh_token';
const ADMIN_CLUBS_KEY = 'uc_admin_clubs';

// ─── Role hierarchy ───────────────────────────────────────────────────────────

const ROLE_HIERARCHY: Record<string, string[]> = {
  PlatformAdmin: ['PlatformAdmin', 'ClubAdmin', 'ClubMember', 'Visitor'],
  ClubAdmin: ['ClubAdmin', 'ClubMember', 'Visitor'],
  ClubMember: ['ClubMember', 'Visitor'],
  Visitor: ['Visitor'],
};

function meetsRole(userRoles: string[], requiredRole: string): boolean {
  return userRoles.some((r) => ROLE_HIERARCHY[r]?.includes(requiredRole));
}

// ─── Store ────────────────────────────────────────────────────────────────────

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  userEmail: string;
  userRoles: string[];
  adminClubIds: number[];

  setTokens: (accessToken: string, refreshToken: string) => void;
  setAdminClubIds: (ids: number[]) => void;
  logout: () => void;

  isAuthenticated: boolean;
  hasRole: (role: string) => boolean;
  isAdmin: (clubId: number) => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => {
  // Hydrate from localStorage
  const savedAccess = localStorage.getItem(ACCESS_TOKEN_KEY);
  const savedRefresh = localStorage.getItem(REFRESH_TOKEN_KEY);
  const savedAdminClubs: number[] = JSON.parse(
    localStorage.getItem(ADMIN_CLUBS_KEY) ?? '[]'
  );
  const initialRoles = savedAccess ? getRoles(savedAccess) : [];
  const initialEmail = savedAccess ? getEmail(savedAccess) : '';

  return {
    accessToken: savedAccess,
    refreshToken: savedRefresh,
    userEmail: initialEmail,
    userRoles: initialRoles,
    adminClubIds: savedAdminClubs,

    setTokens(accessToken, refreshToken) {
      localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
      set({
        accessToken,
        refreshToken,
        userRoles: getRoles(accessToken),
        userEmail: getEmail(accessToken),
        isAuthenticated: true,
      });
    },

    setAdminClubIds(ids) {
      localStorage.setItem(ADMIN_CLUBS_KEY, JSON.stringify(ids));
      set({ adminClubIds: ids });
    },

    logout() {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(ADMIN_CLUBS_KEY);
      set({
        accessToken: null,
        refreshToken: null,
        userEmail: '',
        userRoles: [],
        adminClubIds: [],
        isAuthenticated: false,
      });
    },

    get isAuthenticated() {
      return !!get().accessToken;
    },

    hasRole(role) {
      return meetsRole(get().userRoles, role);
    },

    isAdmin(clubId) {
      return get().adminClubIds.includes(clubId);
    },
  };
});
