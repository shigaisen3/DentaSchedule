import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { doctorApi } from '../../../api/endpoints';
import type { DoctorSchedule, ScheduleException, CreateScheduleRequest, CreateExceptionRequest } from '../../../types';
import Modal from '../../../components/ui/Modal';
import Spinner from '../../../components/ui/Spinner';
import toast from 'react-hot-toast';
import { Plus, Trash2, ArrowLeft } from 'lucide-react';
import { dayName } from '../../../utils/days';

export default function DoctorSchedulePage() {
  const { id: doctorId } = useParams<{ id: string }>();
  const [schedules, setSchedules] = useState<DoctorSchedule[]>([]);
  const [exceptions, setExceptions] = useState<ScheduleException[]>([]);
  const [loading, setLoading] = useState(true);

  // Schedule modal
  const [scheduleModal, setScheduleModal] = useState(false);
  const [scheduleForm, setScheduleForm] = useState<CreateScheduleRequest>({
    dayOfWeek: 1, startTime: '09:00', endTime: '17:00', slotDurationMinutes: 30,
  });

  // Exception modal
  const [exceptionModal, setExceptionModal] = useState(false);
  const [exceptionForm, setExceptionForm] = useState<CreateExceptionRequest>({
    exceptionDate: '', isFullDayOff: true,
  });

  const fetchAll = async () => {
    if (!doctorId) return;
    setLoading(true);
    try {
      const [s, e] = await Promise.all([
        doctorApi.getSchedules(doctorId),
        doctorApi.getExceptions(doctorId),
      ]);
      setSchedules(s.data);
      setExceptions(e.data);
    } catch { toast.error('Failed to load data'); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchAll(); }, [doctorId]);

  const handleAddSchedule = async () => {
    if (!doctorId) return;
    try {
      await doctorApi.createSchedule(doctorId, scheduleForm);
      toast.success('Schedule added');
      setScheduleModal(false);
      fetchAll();
    } catch { toast.error('Failed to add schedule'); }
  };

  const handleDeleteSchedule = async (scheduleId: string) => {
    if (!doctorId || !confirm('Delete this schedule?')) return;
    try {
      await doctorApi.deleteSchedule(doctorId, scheduleId);
      toast.success('Schedule deleted');
      fetchAll();
    } catch { toast.error('Failed to delete'); }
  };

  const handleAddException = async () => {
    if (!doctorId) return;
    try {
      await doctorApi.createException(doctorId, exceptionForm);
      toast.success('Exception added');
      setExceptionModal(false);
      fetchAll();
    } catch { toast.error('Failed to add exception'); }
  };

  const handleDeleteException = async (exceptionId: string) => {
    if (!doctorId || !confirm('Delete this exception?')) return;
    try {
      await doctorApi.deleteException(doctorId, exceptionId);
      toast.success('Exception deleted');
      fetchAll();
    } catch { toast.error('Failed to delete'); }
  };

  const inputClass = 'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:ring-1 focus:ring-teal-500 focus:outline-none';

  if (loading) return <Spinner />;

  return (
    <div>
      <Link to="/admin/doctors" className="mb-4 inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700">
        <ArrowLeft size={16} /> Back to Doctors
      </Link>

      <h1 className="mb-6 text-xl font-bold text-gray-900">Doctor Schedule Configuration</h1>

      {/* Weekly Schedule */}
      <div className="mb-8">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-800">Weekly Schedule</h2>
          <button onClick={() => setScheduleModal(true)}
            className="flex items-center gap-1 rounded-lg bg-teal-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-teal-700">
            <Plus size={14} /> Add
          </button>
        </div>
        {schedules.length > 0 ? (
          <div className="overflow-x-auto rounded-lg border border-gray-200">
            <table className="w-full text-left text-sm">
              <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                <tr>
                  <th className="px-4 py-3">Day</th>
                  <th className="px-4 py-3">Start</th>
                  <th className="px-4 py-3">End</th>
                  <th className="px-4 py-3">Slot (min)</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {schedules.map((s) => (
                  <tr key={s.id}>
                    <td className="px-4 py-3 font-medium">{dayName(s.dayOfWeek)}</td>
                    <td className="px-4 py-3">{s.startTime}</td>
                    <td className="px-4 py-3">{s.endTime}</td>
                    <td className="px-4 py-3">{s.slotDurationMinutes}</td>
                    <td className="px-4 py-3">
                      <button onClick={() => handleDeleteSchedule(s.id)} className="text-red-400 hover:text-red-600">
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : <p className="text-sm text-gray-500">No schedules configured.</p>}
      </div>

      {/* Exceptions */}
      <div>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-800">Schedule Exceptions</h2>
          <button onClick={() => setExceptionModal(true)}
            className="flex items-center gap-1 rounded-lg bg-orange-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-orange-600">
            <Plus size={14} /> Add Exception
          </button>
        </div>
        {exceptions.length > 0 ? (
          <div className="space-y-2">
            {exceptions.map((e) => (
              <div key={e.id} className="flex items-center justify-between rounded-lg border border-gray-200 p-3">
                <div>
                  <span className="font-medium text-gray-900">{e.exceptionDate.split('T')[0]}</span>
                  <span className="ml-2 text-sm text-gray-500">{e.isFullDayOff ? 'Full Day Off' : `${e.customStart} - ${e.customEnd}`}</span>
                  {e.reason && <span className="ml-2 text-xs text-gray-400">({e.reason})</span>}
                </div>
                <button onClick={() => handleDeleteException(e.id)} className="text-red-400 hover:text-red-600">
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>
        ) : <p className="text-sm text-gray-500">No exceptions configured.</p>}
      </div>

      {/* Add Schedule Modal */}
      <Modal open={scheduleModal} onClose={() => setScheduleModal(false)} title="Add Schedule">
        <div className="space-y-3">
          <select value={scheduleForm.dayOfWeek} onChange={(e) => setScheduleForm({ ...scheduleForm, dayOfWeek: +e.target.value })} className={inputClass}>
            {[1, 2, 3, 4, 5, 6, 0].map((d) => <option key={d} value={d}>{dayName(d)}</option>)}
          </select>
          <div className="grid grid-cols-2 gap-3">
            <input type="time" value={scheduleForm.startTime} onChange={(e) => setScheduleForm({ ...scheduleForm, startTime: e.target.value })} className={inputClass} />
            <input type="time" value={scheduleForm.endTime} onChange={(e) => setScheduleForm({ ...scheduleForm, endTime: e.target.value })} className={inputClass} />
          </div>
          <input type="number" placeholder="Slot Duration (minutes)" value={scheduleForm.slotDurationMinutes}
            onChange={(e) => setScheduleForm({ ...scheduleForm, slotDurationMinutes: +e.target.value })} className={inputClass} />
          <div className="flex justify-end gap-2 pt-2">
            <button onClick={() => setScheduleModal(false)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleAddSchedule} className="rounded-lg bg-teal-600 px-4 py-2 text-sm text-white hover:bg-teal-700">Save</button>
          </div>
        </div>
      </Modal>

      {/* Add Exception Modal */}
      <Modal open={exceptionModal} onClose={() => setExceptionModal(false)} title="Add Exception">
        <div className="space-y-3">
          <input type="date" value={exceptionForm.exceptionDate} onChange={(e) => setExceptionForm({ ...exceptionForm, exceptionDate: e.target.value })} className={inputClass} />
          <input placeholder="Reason (optional)" value={exceptionForm.reason ?? ''} onChange={(e) => setExceptionForm({ ...exceptionForm, reason: e.target.value })} className={inputClass} />
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={exceptionForm.isFullDayOff} onChange={(e) => setExceptionForm({ ...exceptionForm, isFullDayOff: e.target.checked })} />
            Full Day Off
          </label>
          {!exceptionForm.isFullDayOff && (
            <div className="grid grid-cols-2 gap-3">
              <input type="time" placeholder="Custom Start" value={exceptionForm.customStart ?? ''}
                onChange={(e) => setExceptionForm({ ...exceptionForm, customStart: e.target.value })} className={inputClass} />
              <input type="time" placeholder="Custom End" value={exceptionForm.customEnd ?? ''}
                onChange={(e) => setExceptionForm({ ...exceptionForm, customEnd: e.target.value })} className={inputClass} />
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button onClick={() => setExceptionModal(false)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleAddException} className="rounded-lg bg-orange-500 px-4 py-2 text-sm text-white hover:bg-orange-600">Save</button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
