// ─── Enums ───────────────────────────────────────────────────────────────────

export type PostInClub =
  | 'Member'
  | 'HeadOfDepartment'
  | 'President'
  | 'Secretary'
  | 'Treasurer'
  | 'VicePresident';

export type AdhesionStatus = 'Pending' | 'Accepted' | 'Refused';

// ─── Auth ────────────────────────────────────────────────────────────────────

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpires: string;
  refreshTokenExpires: string;
}

export interface DecodedToken {
  email: string;
  role: string | string[];
}

// ─── User ────────────────────────────────────────────────────────────────────

export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
}

// ─── Club ────────────────────────────────────────────────────────────────────

export interface Club {
  id: number;
  name: string;
  description: string;
  presidentMail: string | null;
}

export interface UserClub extends Club {
  userPost: PostInClub | null;
}

// ─── Member ──────────────────────────────────────────────────────────────────

export interface Member {
  id: number;
  clubName: string;
  postInClub: PostInClub;
  user: {
    email: string;
    firstName: string;
    lastName: string;
  };
}

// ─── Adhesion ────────────────────────────────────────────────────────────────

export interface Adhesion {
  id: number;
  status: AdhesionStatus;
  clubName: string;
  user: {
    email: string;
    firstName: string;
    lastName: string;
  };
}

// ─── Event ───────────────────────────────────────────────────────────────────

export interface ClubEvent {
  id: number;
  title: string;
  description: string;
  location: string | null;
  startDate: string;
  clubName: string;
}

// ─── Announcement ────────────────────────────────────────────────────────────

export interface Announcement {
  id: number;
  title: string;
  content: string;
  clubName: string;
}

// ─── Pagination ──────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// ─── Chatbot ─────────────────────────────────────────────────────────────────

export interface ChatbotResponse {
  answer: string;
  suggestedActions: { label: string; value: string }[];
  escalate: boolean;
}

export interface Faq {
  id: string;
  question: string;
  answer: string;
  category: string | null;
  created_at: string;
}
