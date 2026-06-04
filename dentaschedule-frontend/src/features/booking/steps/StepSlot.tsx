import { useEffect, useState } from 'react';
import { doctorApi } from '../../../api/endpoints';
import type { TimeSlot } from '../../../types';
import type { BookingData } from '../BookingWizard';
import { format, addDays } from 'date-fns';
import Spinner from '../../../components/ui/Spinner';

interface Props {
  data: BookingData;
  update: (d: Partial<BookingData>) => void;
  next: () => void;
  back: () => void;
}

export default function StepSlot({ data, update, next, back }: Props) {
  const [selectedDate, setSelectedDate] = useState(data.date || format(new Date(), 'yyyy-MM-dd'));
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    doctorApi
      .getAvailableSlots(data.doctorId, selectedDate)
      .then((r) => setSlots(r.data))
      .finally(() => setLoading(false));
  }, [data.doctorId, selectedDate]);

  // Generate next 14 days for date selection
  const dates = Array.from({ length: 14 }, (_, i) => {
    const d = addDays(new Date(), i);
    return { value: format(d, 'yyyy-MM-dd'), label: format(d, 'EEE, MMM d') };
  });

  const handleSelectSlot = (time: string) => {
    update({ date: selectedDate, time });
    next();
  };

  const availableSlots = slots.filter((s) => s.isAvailable);

  return (
    <div>
      <h2 className="mb-4 text-lg font-semibold text-gray-800">Select Date & Time</h2>

      {/* Date picker */}
      <div className="mb-4 flex gap-2 overflow-x-auto pb-2">
        {dates.map((d) => (
          <button
            key={d.value}
            onClick={() => setSelectedDate(d.value)}
            className={`shrink-0 rounded-lg border px-3 py-2 text-xs font-medium transition ${
              selectedDate === d.value
                ? 'border-teal-600 bg-teal-600 text-white'
                : 'border-gray-200 text-gray-600 hover:border-teal-300'
            }`}
          >
            {d.label}
          </button>
        ))}
      </div>

      {/* Time slots */}
      {loading ? (
        <Spinner />
      ) : availableSlots.length > 0 ? (
        <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
          {availableSlots.map((s) => (
            <button
              key={s.time}
              onClick={() => handleSelectSlot(s.time)}
              className={`rounded-lg border px-3 py-2 text-sm font-medium transition hover:border-teal-500 hover:bg-teal-50 ${
                data.time === s.time && data.date === selectedDate
                  ? 'border-teal-600 bg-teal-600 text-white'
                  : 'border-gray-200 text-gray-700'
              }`}
            >
              {s.time}
            </button>
          ))}
        </div>
      ) : (
        <p className="py-8 text-center text-gray-500">No available slots for this date.</p>
      )}

      <button onClick={back} className="mt-4 text-sm text-gray-500 hover:text-gray-700">&larr; Back</button>
    </div>
  );
}
