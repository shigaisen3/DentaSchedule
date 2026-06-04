import { useEffect, useState, useCallback } from 'react';
import { appointmentApi } from '../../../api/endpoints';
import type { Appointment, PagedResult } from '../../../types';
import StatusBadge from '../../../components/ui/StatusBadge';
import Spinner from '../../../components/ui/Spinner';
import Modal from '../../../components/ui/Modal';
import CalendarView from './CalendarView';
import toast from 'react-hot-toast';
import { format, addDays, startOfWeek } from 'date-fns';
import { LayoutList, CalendarDays } from 'lucide-react';

export default function AppointmentsPage() {
  // View
  const [view, setView] = useState<'list' | 'calendar'>('list');

  // List state
  const [data, setData] = useState<PagedResult<Appointment> | null>(null);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  // Calendar state
  const [calendarWeekStart, setCalendarWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  );
  const [calendarAppointments, setCalendarAppointments] = useState<Appointment[]>([]);
  const [calendarLoading, setCalendarLoading] = useState(false);

  // Shared action state
  const [cancelTarget, setCancelTarget] = useState<string | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [cancelling, setCancelling] = useState(false);
  const [rescheduleTarget, setRescheduleTarget] = useState<string | null>(null);
  const [newDateTime, setNewDateTime] = useState('');
  const [rescheduling, setRescheduling] = useState(false);
  const [approvingId, setApprovingId] = useState<string | null>(null);

  // ── Fetch helpers ──────────────────────────────────────────────────────────

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await appointmentApi.getAll({ status: statusFilter || undefined, page, pageSize: 15 });
      setData(res.data);
    } catch {
      toast.error('Failed to load appointments');
    } finally {
      setLoading(false);
    }
  }, [statusFilter, page]);

  const fetchCalendarData = useCallback(async (weekStart: Date) => {
    setCalendarLoading(true);
    try {
      const dateFrom = format(weekStart, "yyyy-MM-dd'T'00:00:00");
      const dateTo   = format(addDays(weekStart, 7), "yyyy-MM-dd'T'00:00:00");
      const res = await appointmentApi.getAll({
        status: statusFilter || undefined,
        dateFrom,
        dateTo,
        pageSize: 200,
      });
      setCalendarAppointments(res.data.items);
    } catch {
      toast.error('Failed to load calendar');
    } finally {
      setCalendarLoading(false);
    }
  }, [statusFilter]);

  const refresh = useCallback(() => {
    if (view === 'calendar') fetchCalendarData(calendarWeekStart);
    else fetchData();
  }, [view, calendarWeekStart, fetchCalendarData, fetchData]);

  useEffect(() => {
    if (view === 'list') fetchData();
  }, [fetchData, view]);

  useEffect(() => {
    if (view === 'calendar') fetchCalendarData(calendarWeekStart);
  }, [view, calendarWeekStart, fetchCalendarData]);

  // ── Action handlers ────────────────────────────────────────────────────────

  const handleApprove = async (id: string) => {
    setApprovingId(id);
    try {
      await appointmentApi.approve(id);
      toast.success('Appointment approved');
      refresh();
    } catch {
      toast.error('Failed to approve');
    } finally {
      setApprovingId(null);
    }
  };

  const handleCancel = async () => {
    if (!cancelTarget || !cancelReason) return;
    setCancelling(true);
    try {
      await appointmentApi.cancel(cancelTarget, cancelReason);
      toast.success('Appointment cancelled');
      setCancelTarget(null);
      setCancelReason('');
      refresh();
    } catch {
      toast.error('Failed to cancel');
    } finally {
      setCancelling(false);
    }
  };

  const handleReschedule = async () => {
    if (!rescheduleTarget || !newDateTime) return;
    setRescheduling(true);
    try {
      await appointmentApi.reschedule(rescheduleTarget, newDateTime);
      toast.success('Appointment rescheduled');
      setRescheduleTarget(null);
      setNewDateTime('');
      refresh();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors?.[0];
      toast.error(msg || 'Failed to reschedule');
    } finally {
      setRescheduling(false);
    }
  };

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div>
      {/* Header + view toggle */}
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">Appointments</h1>
        <div className="flex rounded-lg border border-gray-200 p-0.5">
          <button
            onClick={() => { setView('list'); setPage(1); }}
            className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-medium transition ${
              view === 'list' ? 'bg-teal-600 text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <LayoutList size={13} /> List
          </button>
          <button
            onClick={() => setView('calendar')}
            className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-medium transition ${
              view === 'calendar' ? 'bg-teal-600 text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <CalendarDays size={13} /> Calendar
          </button>
        </div>
      </div>

      {/* Status filter tabs */}
      <div className="mb-4 flex gap-2">
        {['', 'Pending', 'Approved', 'Cancelled'].map((s) => (
          <button
            key={s}
            onClick={() => { setStatusFilter(s); setPage(1); }}
            className={`rounded-lg px-3 py-1.5 text-xs font-medium transition ${
              statusFilter === s ? 'bg-teal-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {s || 'All'}
          </button>
        ))}
      </div>

      {/* Calendar view */}
      {view === 'calendar' && (
        <CalendarView
          appointments={calendarAppointments}
          weekStart={calendarWeekStart}
          onWeekChange={setCalendarWeekStart}
          onApprove={handleApprove}
          onCancelClick={setCancelTarget}
          onRescheduleClick={setRescheduleTarget}
          approvingId={approvingId}
          loading={calendarLoading}
        />
      )}

      {/* List view */}
      {view === 'list' && (
        loading ? (
          <Spinner />
        ) : !data || data.items.length === 0 ? (
          <p className="py-8 text-center text-gray-500">No appointments found.</p>
        ) : (
          <>
            <div className="overflow-x-auto rounded-lg border border-gray-200">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                  <tr>
                    <th className="px-4 py-3">Reference</th>
                    <th className="px-4 py-3">Patient</th>
                    <th className="px-4 py-3">Doctor</th>
                    <th className="px-4 py-3">Date/Time</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.items.map((a) => (
                    <>
                      <tr
                        key={a.id}
                        onClick={() => setExpandedId(expandedId === a.id ? null : a.id)}
                        className="cursor-pointer hover:bg-gray-50"
                      >
                        <td className="px-4 py-3 font-mono text-xs">{a.referenceNumber}</td>
                        <td className="px-4 py-3">
                          <div className="font-medium">{a.patientName}</div>
                          <div className="text-xs text-gray-400">{a.patientEmail}</div>
                        </td>
                        <td className="px-4 py-3">{a.doctorName}</td>
                        <td className="px-4 py-3 text-xs">
                          {format(new Date(a.appointmentDateTime), 'MMM d, yyyy HH:mm')}
                        </td>
                        <td className="px-4 py-3"><StatusBadge status={a.status} /></td>
                        <td className="px-4 py-3">
                          <div className="flex gap-1" onClick={(e) => e.stopPropagation()}>
                            {a.status === 'Pending' && (
                              <button
                                onClick={() => handleApprove(a.id)}
                                disabled={approvingId === a.id}
                                className="rounded bg-green-100 px-2 py-1 text-xs font-medium text-green-700 hover:bg-green-200 disabled:opacity-50"
                              >
                                {approvingId === a.id ? 'Approving…' : 'Approve'}
                              </button>
                            )}
                            {a.status !== 'Cancelled' && (
                              <>
                                <button
                                  onClick={() => setCancelTarget(a.id)}
                                  className="rounded bg-red-100 px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-200"
                                >
                                  Cancel
                                </button>
                                <button
                                  onClick={() => setRescheduleTarget(a.id)}
                                  className="rounded bg-blue-100 px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-200"
                                >
                                  Reschedule
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                      {expandedId === a.id && (
                        <tr key={`${a.id}-expanded`} className="bg-gray-50">
                          <td colSpan={6} className="px-6 py-4">
                            <div className="grid grid-cols-2 gap-x-8 gap-y-2 text-sm sm:grid-cols-4">
                              <div>
                                <p className="text-xs font-medium uppercase text-gray-400">Phone</p>
                                <p className="text-gray-700">{a.patientPhone}</p>
                              </div>
                              <div>
                                <p className="text-xs font-medium uppercase text-gray-400">Clinic</p>
                                <p className="text-gray-700">{a.clinicName ?? '—'}</p>
                              </div>
                              <div>
                                <p className="text-xs font-medium uppercase text-gray-400">Duration</p>
                                <p className="text-gray-700">{a.durationMinutes} min</p>
                              </div>
                              <div>
                                <p className="text-xs font-medium uppercase text-gray-400">Booked At</p>
                                <p className="text-gray-700">{format(new Date(a.createdAt), 'MMM d, yyyy HH:mm')}</p>
                              </div>
                              <div className="col-span-2 sm:col-span-4">
                                <p className="text-xs font-medium uppercase text-gray-400">Notes</p>
                                <p className="text-gray-700">{a.notes?.trim() || <span className="italic text-gray-400">No notes provided</span>}</p>
                              </div>
                              {a.cancellationReason && (
                                <div className="col-span-2 sm:col-span-4">
                                  <p className="text-xs font-medium uppercase text-red-400">Cancellation Reason</p>
                                  <p className="text-red-600">{a.cancellationReason}</p>
                                </div>
                              )}
                            </div>
                          </td>
                        </tr>
                      )}
                    </>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-4 flex items-center justify-between text-sm text-gray-500">
              <span>Page {data.page} of {data.totalPages} ({data.totalCount} total)</span>
              <div className="flex gap-2">
                <button disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}
                  className="rounded border px-3 py-1 disabled:opacity-30">Previous</button>
                <button disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}
                  className="rounded border px-3 py-1 disabled:opacity-30">Next</button>
              </div>
            </div>
          </>
        )
      )}

      {/* Cancel Modal */}
      <Modal open={!!cancelTarget} onClose={() => setCancelTarget(null)} title="Cancel Appointment">
        <div className="space-y-3">
          <label className="block text-sm font-medium text-gray-700">Reason *</label>
          <textarea value={cancelReason} onChange={(e) => setCancelReason(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none" rows={3} />
          <div className="flex justify-end gap-2">
            <button onClick={() => setCancelTarget(null)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleCancel} disabled={!cancelReason || cancelling}
              className="rounded-lg bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700 disabled:opacity-50">
              {cancelling ? 'Cancelling…' : 'Confirm Cancel'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Reschedule Modal */}
      <Modal open={!!rescheduleTarget} onClose={() => setRescheduleTarget(null)} title="Reschedule Appointment">
        <div className="space-y-3">
          <label className="block text-sm font-medium text-gray-700">New Date & Time *</label>
          <input type="datetime-local" value={newDateTime} onChange={(e) => setNewDateTime(e.target.value)}
            min={format(new Date(), "yyyy-MM-dd'T'HH:mm")}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none" />
          <div className="flex justify-end gap-2">
            <button onClick={() => setRescheduleTarget(null)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleReschedule} disabled={!newDateTime || rescheduling}
              className="rounded-lg bg-teal-600 px-4 py-2 text-sm text-white hover:bg-teal-700 disabled:opacity-50">
              {rescheduling ? 'Rescheduling…' : 'Confirm Reschedule'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
