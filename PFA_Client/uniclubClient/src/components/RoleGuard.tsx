import { useAuthStore } from '../stores/authStore';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
  requiredRole?: string;
  clubId?: number;
}

/**
 * Renders children only when the user meets the role requirement.
 * If clubId is provided, additionally checks club-scoped admin access.
 */
export default function RoleGuard({ children, requiredRole, clubId }: Props) {
  const { hasRole, isAdmin } = useAuthStore();

  if (requiredRole && !hasRole(requiredRole)) return null;
  if (clubId !== undefined && !isAdmin(clubId)) return null;

  return <>{children}</>;
}
