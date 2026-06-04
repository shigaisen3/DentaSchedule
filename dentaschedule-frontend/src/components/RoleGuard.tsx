import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';

interface Props {
  roles: string[];
}

export default function RoleGuard({ roles }: Props) {
  const { user } = useAuth();
  const hasAccess = user?.roles.some((r) => roles.includes(r));
  return hasAccess ? <Outlet /> : <Navigate to="/admin/appointments" replace />;
}
