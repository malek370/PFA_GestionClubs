import { jwtDecode } from 'jwt-decode';

// .NET can emit roles under different claim keys depending on configuration
const ROLE_CLAIM_KEYS = [
  'role',
  'roles',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role',
];

const EMAIL_CLAIM_KEYS = [
  'email',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  'sub',
];

export function getRoles(token: string): string[] {
  try {
    const decoded = jwtDecode<Record<string, unknown>>(token);
    for (const key of ROLE_CLAIM_KEYS) {
      const val = decoded[key];
      if (!val) continue;
      if (Array.isArray(val)) return val as string[];
      if (typeof val === 'string') return [val];
    }
    return [];
  } catch {
    return [];
  }
}

export function getEmail(token: string): string {
  try {
    const decoded = jwtDecode<Record<string, unknown>>(token);
    for (const key of EMAIL_CLAIM_KEYS) {
      const val = decoded[key];
      if (typeof val === 'string' && val) return val;
    }
    return '';
  } catch {
    return '';
  }
}

export function isTokenExpired(token: string): boolean {
  try {
    const { exp } = jwtDecode<{ exp?: number }>(token);
    if (exp === undefined) return false;
    return Date.now() >= exp * 1000;
  } catch {
    return true;
  }
}
