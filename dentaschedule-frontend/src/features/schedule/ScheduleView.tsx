import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { doctorApi } from '../../api/endpoints';
import type { TimeSlot } from '../../types';
import { format, addDays } from 'date-fns';
import Spinner from '../../components/ui/Spinner';
import { cn } from '../../utils/cn';

export default function ScheduleView() {
  const { clinicId, doctorId } = useParams<{ clinicId: string; doctorId: string }>();
  const [selectedDate, setSelectedDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!doctorId) return;
    setLoading(true);
    doctorApi
      .getAvailableSlots(doctorId, selectedDate)
      .then((r) => setSlots(r.data))
      .finally(() => setLoading(false));
  }, [doctorId, selectedDate]);

  const dates = Array.from({ length: 7 }, (_, i) => {
    const d = addDays(new Date(), i);
    return { value: format(d, 'yyyy-MM-dd'), label: format(d, 'EEE, MMM d') };
  });

  return (
    <div className="mx-auto min-h-screen max-w-2xl bg-white px-4 py-8">
      <h1 className="mb-2 text-xl font-bold text-teal-700">Schedule Availability</h1>
      <p className="mb-6 text-sm text-gray-500">Clinic: {clinicId} &middot; Doctor: {doctorId}</p>

      {/* Date selector */}
      <div className="mb-6 flex gap-2 overflow-x-auto pb-2">
        {dates.map((d) => (
          <button
            key={d.value}
            onClick={() => setSelectedDate(d.value)}
            className={cn(
              'shrink-0 rounded-lg border px-3 py-2 text-xs font-medium transition',
              selectedDate === d.value
                ? 'border-teal-600 bg-teal-600 text-white'
                : 'border-gray-200 text-gray-600 hover:border-teal-300'
            )}
          >
            {d.label}
          </button>
        ))}
      </div>

      {/* Slots grid */}
      {loading ? (
        <Spinner />
      ) : slots.length > 0 ? (
        <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
          {slots.map((s) => (
            <div
              key={s.time}
              className={cn(
                'rounded-lg border px-3 py-2 text-center text-sm font-medium',
                s.isAvailable
                  ? 'border-green-200 bg-green-50 text-green-700'
                  : 'border-gray-200 bg-gray-100 text-gray-400 line-through'
              )}
            >
              {s.time}
            </div>
          ))}
        </div>
      ) : (
        <p className="py-8 text-center text-gray-500">No schedule available for this date.</p>
      )}

      {/* Legend */}
      <div className="mt-6 flex gap-4 text-xs text-gray-500">
        <div className="flex items-center gap-1">
          <div className="h-3 w-3 rounded bg-green-200" /> Available
        </div>
        <div className="flex items-center gap-1">
          <div className="h-3 w-3 rounded bg-gray-200" /> Occupied
        </div>
      </div>
    </div>
  );
}
