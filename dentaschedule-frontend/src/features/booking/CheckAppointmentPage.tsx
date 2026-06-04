import { useState, useEffect } from 'react';
import { appointmentApi } from '../../api/endpoints';
import type { Appointment } from '../../types';
import StatusBadge from '../../components/ui/StatusBadge';
import { format } from 'date-fns';
import { Search, CalendarDays, Clock, User, Phone, Mail, Stethoscope, Building2, FileText } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';

export default function CheckAppointmentPage() {
  const [searchParams] = useSearchParams();
  const [reference, setReference] = useState(searchParams.get('ref') ?? '');
  const [appointment, setAppointment] = useState<Appointment | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Auto-search if ref param is present
  useEffect(() => {
    const ref = searchParams.get('ref');
    if (ref) {
      setReference(ref);
      doSearch(ref);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const doSearch = async (ref: string) => {
    const normalized = ref.trim().toUpperCase();
    if (!normalized) return;

    setLoading(true);
    setError('');
    setAppointment(null);

    try {
      const res = await appointmentApi.getByReference(normalized);
      setAppointment(res.data);
    } catch {
      setError('No appointment found with this reference number. Please check and try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    doSearch(reference);
  };

  return (
    <div className="mx-auto min-h-screen max-w-lg px-4 py-12">
      {/* Header */}
      <div className="mb-8 text-center">
        <h1 className="text-2xl font-bold text-teal-700">DentaSchedule</h1>
        <p className="mt-1 text-sm text-gray-500">Check Your Appointment Status</p>
      </div>

      {/* Search form */}
      <form onSubmit={handleSearch} className="mb-6">
        <label className="mb-1 block text-sm font-medium text-gray-700">
          Reference Number
        </label>
        <div className="flex gap-2">
          <input
            type="text"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            placeholder="e.g. DS-12345678"
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-mono uppercase tracking-wider focus:border-teal-500 focus:outline-none focus:ring-1 focus:ring-teal-500"
          />
          <button
            type="submit"
            disabled={loading || !reference.trim()}
            className="flex items-center gap-2 rounded-lg bg-teal-600 px-4 py-2 text-sm font-medium text-white hover:bg-teal-700 disabled:opacity-50"
          >
            <Search size={16} />
            {loading ? 'Searching...' : 'Search'}
          </button>
        </div>
      </form>

      {/* Error */}
      {error && (
        <div className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {/* Result */}
      {appointment && (
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm">
          {/* Status banner */}
          <div className={`rounded-t-xl px-6 py-4 ${
            appointment.status === 'Approved' ? 'bg-green-50' :
            appointment.status === 'Cancelled' ? 'bg-red-50' : 'bg-yellow-50'
          }`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs font-medium uppercase text-gray-500">Reference</p>
                <p className="font-mono text-lg font-bold text-gray-800">{appointment.referenceNumber}</p>
              </div>
              <StatusBadge status={appointment.status} />
            </div>
            {appointment.status === 'Pending' && (
              <p className="mt-2 text-xs text-yellow-700">Your appointment is awaiting confirmation from the clinic.</p>
            )}
            {appointment.status === 'Approved' && (
              <p className="mt-2 text-xs text-green-700">Your appointment has been confirmed. Please arrive on time.</p>
            )}
            {appointment.status === 'Cancelled' && (
              <p className="mt-2 text-xs text-red-700">This appointment has been cancelled.</p>
            )}
          </div>

          {/* Details */}
          <div className="divide-y divide-gray-100 px-6 py-2">
            <DetailRow icon={<CalendarDays size={16} className="text-teal-600" />} label="Date & Time">
              {format(new Date(appointment.appointmentDateTime), 'EEEE, MMMM d, yyyy')}
              <span className="ml-2 text-gray-500">at {format(new Date(appointment.appointmentDateTime), 'HH:mm')}</span>
            </DetailRow>

            <DetailRow icon={<Clock size={16} className="text-teal-600" />} label="Duration">
              {appointment.durationMinutes} minutes
            </DetailRow>

            <DetailRow icon={<Stethoscope size={16} className="text-teal-600" />} label="Doctor">
              {appointment.doctorName ?? '—'}
            </DetailRow>

            <DetailRow icon={<Building2 size={16} className="text-teal-600" />} label="Clinic">
              {appointment.clinicName ?? '—'}
            </DetailRow>

            <DetailRow icon={<User size={16} className="text-teal-600" />} label="Patient">
              {appointment.patientName}
            </DetailRow>

            <DetailRow icon={<Mail size={16} className="text-teal-600" />} label="Email">
              {appointment.patientEmail}
            </DetailRow>

            <DetailRow icon={<Phone size={16} className="text-teal-600" />} label="Phone">
              {appointment.patientPhone}
            </DetailRow>

            {appointment.notes && (
              <DetailRow icon={<FileText size={16} className="text-teal-600" />} label="Notes">
                {appointment.notes}
              </DetailRow>
            )}

            {appointment.cancellationReason && (
              <DetailRow icon={<FileText size={16} className="text-red-500" />} label="Cancellation Reason">
                <span className="text-red-600">{appointment.cancellationReason}</span>
              </DetailRow>
            )}
          </div>

          <div className="px-6 py-3 text-xs text-gray-400">
            Booked on {format(new Date(appointment.createdAt), 'MMM d, yyyy HH:mm')}
          </div>
        </div>
      )}

      {/* Footer links */}
      <div className="mt-8 flex justify-center gap-4 text-sm text-gray-500">
        <Link to="/booking" className="hover:text-teal-600">Book an Appointment</Link>
        <span>·</span>
        <Link to="/login" className="hover:text-teal-600">Staff Login</Link>
      </div>
    </div>
  );
}

function DetailRow({ icon, label, children }: { icon: React.ReactNode; label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3 py-3">
      <div className="mt-0.5 shrink-0">{icon}</div>
      <div>
        <p className="text-xs font-medium text-gray-400">{label}</p>
        <p className="text-sm text-gray-800">{children}</p>
      </div>
    </div>
  );
}
