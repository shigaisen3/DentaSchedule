import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';
import {
  LayoutDashboard, CalendarDays, Building2, Stethoscope,
  Users, LogOut, ChevronLeft, ChevronRight, Menu, X,
} from 'lucide-react';
import { cn } from '../utils/cn';

interface NavItem {
  to: string;
  icon: React.ReactNode;
  label: string;
  adminOnly?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { to: '/admin/dashboard',    icon: <LayoutDashboard size={18} />, label: 'Dashboard' },
  { to: '/admin/appointments', icon: <CalendarDays size={18} />,    label: 'Appointments' },
  { to: '/admin/clinics',      icon: <Building2 size={18} />,       label: 'Clinics',   adminOnly: true },
  { to: '/admin/doctors',      icon: <Stethoscope size={18} />,     label: 'Doctors',   adminOnly: true },
  { to: '/admin/users',        icon: <Users size={18} />,           label: 'Users',     adminOnly: true },
];

export default function AdminLayout() {
  const { user, logout, hasRole } = useAuth();
  const location = useLocation();

  const [collapsed, setCollapsed] = useState(false);   // desktop
  const [mobileOpen, setMobileOpen] = useState(false); // mobile drawer

  // Close mobile drawer on navigation
  useEffect(() => { setMobileOpen(false); }, [location.pathname]);

  const navLinkClass = (isActive: boolean) =>
    cn(
      'flex items-center rounded-lg py-2 text-sm font-medium transition-colors',
      collapsed ? 'justify-center px-2' : 'gap-3 px-3',
      isActive ? 'bg-teal-50 text-teal-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
    );

  const SidebarContent = ({ mobile = false }: { mobile?: boolean }) => (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className={cn('border-b border-gray-200 p-4', collapsed && !mobile ? 'px-2 text-center' : '')}>
        {collapsed && !mobile ? (
          <span className="text-lg font-bold text-teal-700">DS</span>
        ) : (
          <>
            <div className="flex items-center justify-between">
              <h1 className="text-xl font-bold text-teal-700">DentaSchedule</h1>
              {mobile && (
                <button onClick={() => setMobileOpen(false)} className="rounded p-1 text-gray-400 hover:text-gray-600">
                  <X size={18} />
                </button>
              )}
            </div>
            {user?.clinicName && (
              <p className="mt-1 flex items-center gap-1 text-sm font-medium text-teal-600">
                <Building2 size={13} />
                {user.clinicName}
              </p>
            )}
            <p className="mt-0.5 text-xs text-gray-400">{user?.displayName}</p>
          </>
        )}
      </div>

      {/* Nav */}
      <nav className={cn('flex-1 space-y-1 p-2', collapsed && !mobile ? 'px-1' : 'p-3')}>
        {NAV_ITEMS.filter((item) => !item.adminOnly || hasRole('Admin')).map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            title={collapsed && !mobile ? item.label : undefined}
            className={({ isActive }) => navLinkClass(isActive)}
          >
            {item.icon}
            {(!collapsed || mobile) && item.label}
          </NavLink>
        ))}
      </nav>

      {/* Collapse toggle (desktop only) */}
      {!mobile && (
        <div className="border-t border-gray-200 p-2">
          <button
            onClick={() => setCollapsed((c) => !c)}
            className={cn(
              'flex w-full items-center rounded-lg px-3 py-2 text-xs font-medium text-gray-400 hover:bg-gray-50 hover:text-gray-600',
              collapsed ? 'justify-center px-2' : 'gap-2'
            )}
          >
            {collapsed ? <ChevronRight size={16} /> : <><ChevronLeft size={16} /> Collapse</>}
          </button>
        </div>
      )}

      {/* Logout */}
      <div className={cn('border-t border-gray-200 p-2', !mobile && 'border-t-0')}>
        <button
          onClick={logout}
          title={collapsed && !mobile ? 'Logout' : undefined}
          className={cn(
            'flex w-full items-center rounded-lg py-2 text-sm font-medium text-gray-600 hover:bg-red-50 hover:text-red-600',
            collapsed && !mobile ? 'justify-center px-2' : 'gap-3 px-3'
          )}
        >
          <LogOut size={18} />
          {(!collapsed || mobile) && 'Logout'}
        </button>
      </div>
    </div>
  );

  return (
    <div className="flex h-screen bg-gray-50">

      {/* Mobile backdrop */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Desktop sidebar */}
      <aside
        className={cn(
          'hidden lg:flex flex-col border-r border-gray-200 bg-white transition-all duration-300',
          collapsed ? 'w-16' : 'w-64'
        )}
      >
        <SidebarContent />
      </aside>

      {/* Mobile sidebar (slide-in drawer) */}
      <aside
        className={cn(
          'fixed top-0 left-0 z-40 flex h-full w-64 flex-col border-r border-gray-200 bg-white transition-transform duration-300 lg:hidden',
          mobileOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <SidebarContent mobile />
      </aside>

      {/* Main area */}
      <div className="flex flex-1 flex-col overflow-hidden">

        {/* Mobile top bar */}
        <div className="flex items-center gap-3 border-b border-gray-200 bg-white px-4 py-3 lg:hidden">
          <button
            onClick={() => setMobileOpen(true)}
            className="rounded-lg p-1.5 text-gray-500 hover:bg-gray-100"
          >
            <Menu size={20} />
          </button>
          <span className="font-bold text-teal-700">DentaSchedule</span>
          {user?.clinicName && (
            <span className="ml-1 flex items-center gap-1 text-xs font-medium text-teal-600">
              <Building2 size={11} /> {user.clinicName}
            </span>
          )}
        </div>

        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
