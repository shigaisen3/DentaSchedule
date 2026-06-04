import { useEffect, useState } from 'react';
import { appointmentApi } from '../../api/endpoints';
import type { DashboardStats } from '../../types';
import StatusBadge from '../../components/ui/StatusBadge';
import Spinner from '../../components/ui/Spinner';
import { CalendarDays, Clock, CheckCircle, XCircle, Building2, Stethoscope, CalendarRange } from 'lucide-react';
import { format } from 'date-fns';
import { useAuth } from '../../store/AuthContext';

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  useEffect(() => {
    appointmentApi.getStats()
      .then((r) => setStats(r.data))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner />;
  if (!stats) return null;

  const today = format(new Date(), 'EEEE, MMMM d, yyyy');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-sm text-gray-500">Welcome back, {user?.displayName} · {today}</p>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          icon={<Clock size={20} className="text-yellow-600" />}
          bg="bg-yellow-50"
          label="Pending Today"
          value={stats.pendingToday}
        />
        <StatCard
          icon={<CheckCircle size={20} className="text-green-600" />}
          bg="bg-green-50"
          label="Approved Today"
          value={stats.approvedToday}
        />
        <StatCard
          icon={<XCircle size={20} className="text-red-500" />}
          bg="bg-red-50"
          label="Cancelled Today"
          value={stats.cancelledToday}
        />
        <StatCard
          icon={<CalendarRange size={20} className="text-teal-600" />}
          bg="bg-teal-50"
          label="This Week"
          value={stats.thisWeek}
        />
      </div>

      {/* Secondary cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard
          icon={<CalendarDays size={20} className="text-blue-600" />}
          bg="bg-blue-50"
          label="Total Appointments"
          value={stats.totalAppointments}
        />
        <StatCard
          icon={<Building2 size={20} className="text-purple-600" />}
          bg="bg-purple-50"
          label="Clinics"
          value={stats.totalClinics}
        />
        <StatCard
          icon={<Stethoscope size={20} className="text-indigo-600" />}
          bg="bg-indigo-50"
          label="Doctors"
          value={stats.totalDoctors}
        />
      </div>

      {/* Recent appointments */}
      <div className="rounded-xl border border-gray-200 bg-white">
        <div className="border-b border-gray-100 px-5 py-4">
          <h2 className="text-sm font-semibold text-gray-800">Recent Appointments</h2>
        </div>
        {stats.recentAppointments.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-gray-400">No appointments yet.</p>
        ) : (
          <div className="divide-y divide-gray-50">
            {stats.recentAppointments.map((a) => (
              <div key={a.referenceNumber} className="flex items-center justify-between px-5 py-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-xs text-gray-400">{a.referenceNumber}</span>
                    <StatusBadge status={a.status} />
                  </div>
                  <p className="mt-0.5 truncate text-sm font-medium text-gray-800">{a.patientName}</p>
                  <p className="text-xs text-gray-400">{a.doctorName} · {a.clinicName}</p>
                </div>
                <div className="ml-4 shrink-0 text-right">
                  <p className="text-xs font-medium text-gray-600">
                    {format(new Date(a.appointmentDateTime), 'MMM d')}
                  </p>
                  <p className="text-xs text-gray-400">
                    {format(new Date(a.appointmentDateTime), 'HH:mm')}
                  </p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function StatCard({ icon, bg, label, value }: {
  icon: React.ReactNode;
  bg: string;
  label: string;
  value: number;
}) {
  return (
    <div className="flex items-center gap-4 rounded-xl border border-gray-200 bg-white p-4">
      <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${bg}`}>
        {icon}
      </div>
      <div>
        <p className="text-xs text-gray-500">{label}</p>
        <p className="text-2xl font-bold text-gray-900">{value}</p>
      </div>
    </div>
  );
}
