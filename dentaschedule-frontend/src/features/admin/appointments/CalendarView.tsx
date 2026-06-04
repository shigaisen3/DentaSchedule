import { useState, useEffect } from 'react';
import { format, addDays, isToday, getHours, getMinutes, startOfWeek } from 'date-fns';
import type { Appointment } from '../../../types';
import StatusBadge from '../../../components/ui/StatusBadge';
import Modal from '../../../components/ui/Modal';

const HOUR_START = 8;
const HOUR_END = 18;
const SLOT_HEIGHT = 40; // px per 30-min slot
const TOTAL_SLOTS = (HOUR_END - HOUR_START) * 2; // 20 slots
const TOTAL_HEIGHT = TOTAL_SLOTS * SLOT_HEIGHT;   // 800px

interface Props {
  appointments: Appointment[];
  weekStart: Date;
  onWeekChange: (d: Date) => void;
  onApprove: (id: string) => void;
  onCancelClick: (id: string) => void;
  onRescheduleClick: (id: string) => void;
  approvingId: string | null;
  loading: boolean;
}

interface LayoutAppt {
  appt: Appointment;
  col: number;
  totalCols: number;
}

/** Assigns non-overlapping appointments to columns so they sit side-by-side. */
function layoutDay(appts: Appointment[]): LayoutAppt[] {
  if (appts.length === 0) return [];

  const sorted = [...appts].sort(
    (a, b) => new Date(a.appointmentDateTime).getTime() - new Date(b.appointmentDateTime).getTime()
  );

  const colEnds: number[] = [];
  const assigned: { appt: Appointment; col: number }[] = [];

  for (const appt of sorted) {
    const start = new Date(appt.appointmentDateTime).getTime();
    const end = start + appt.durationMinutes * 60_000;
    let placed = false;
    for (let c = 0; c < colEnds.length; c++) {
      if (start >= colEnds[c]) {
        colEnds[c] = end;
        assigned.push({ appt, col: c });
        placed = true;
        break;
      }
    }
    if (!placed) {
      assigned.push({ appt, col: colEnds.length });
      colEnds.push(end);
    }
  }

  return assigned.map(({ appt, col }) => {
    const start = new Date(appt.appointmentDateTime).getTime();
    const end = start + appt.durationMinutes * 60_000;
    const overlapping = assigned.filter(({ appt: o }) => {
      const oStart = new Date(o.appointmentDateTime).getTime();
      const oEnd = oStart + o.durationMinutes * 60_000;
      return oStart < end && oEnd > start;
    });
    const totalCols = Math.max(...overlapping.map((o) => o.col)) + 1;
    return { appt, col, totalCols };
  });
}

const STATUS_CLASSES: Record<string, string> = {
  Pending:   'bg-amber-50  border-l-amber-400 text-amber-900',
  Approved:  'bg-teal-50   border-l-teal-500  text-teal-900',
  Cancelled: 'bg-red-50    border-l-red-300   text-red-500 opacity-50',
};

/** Returns 3 on small screens, 7 on desktop. Updates on resize. */
function useDaysToShow(): number {
  const [daysToShow, setDaysToShow] = useState(() =>
    typeof window !== 'undefined' && window.innerWidth < 768 ? 3 : 7
  );
  useEffect(() => {
    const handler = () => setDaysToShow(window.innerWidth < 768 ? 3 : 7);
    window.addEventListener('resize', handler);
    return () => window.removeEventListener('resize', handler);
  }, []);
  return daysToShow;
}

export default function CalendarView({
  appointments, weekStart, onWeekChange,
  onApprove, onCancelClick, onRescheduleClick,
  approvingId, loading,
}: Props) {
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [now, setNow] = useState(new Date());
  const daysToShow = useDaysToShow();

  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(t);
  }, []);

  const days = Array.from({ length: daysToShow }, (_, i) => addDays(weekStart, i));

  const slots = Array.from({ length: TOTAL_SLOTS }, (_, i) => {
    const h = HOUR_START + Math.floor(i / 2);
    const m = i % 2 === 0 ? '00' : '30';
    return `${String(h).padStart(2, '0')}:${m}`;
  });

  const topFor = (dt: Date) => {
    const mins = (getHours(dt) - HOUR_START) * 60 + getMinutes(dt);
    return (mins / 30) * SLOT_HEIGHT;
  };

  const heightFor = (mins: number) =>
    Math.max(SLOT_HEIGHT * 0.85, (mins / 30) * SLOT_HEIGHT);

  const nowTop = (() => {
    const mins = (getHours(now) - HOUR_START) * 60 + getMinutes(now);
    if (mins < 0 || mins > (HOUR_END - HOUR_START) * 60) return null;
    return (mins / 30) * SLOT_HEIGHT;
  })();

  const apptsByDay = (day: Date) =>
    appointments.filter(
      (a) => format(new Date(a.appointmentDateTime), 'yyyy-MM-dd') === format(day, 'yyyy-MM-dd')
    );

  const handlePrev = () => onWeekChange(addDays(weekStart, -daysToShow));
  const handleNext = () => onWeekChange(addDays(weekStart, daysToShow));
  const handleToday = () =>
    onWeekChange(
      daysToShow === 7
        ? startOfWeek(new Date(), { weekStartsOn: 1 })
        : new Date()
    );

  return (
    <div>
      {/* Navigation */}
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <div className="flex gap-1">
          <button
            onClick={handlePrev}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm hover:bg-gray-50"
          >
            ← Prev
          </button>
          <button
            onClick={handleToday}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm hover:bg-gray-50"
          >
            Today
          </button>
          <button
            onClick={handleNext}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm hover:bg-gray-50"
          >
            Next →
          </button>
        </div>
        <span className="text-sm font-semibold text-gray-700">
          {format(weekStart, 'MMM d')} – {format(addDays(weekStart, daysToShow - 1), 'MMM d, yyyy')}
        </span>
        {loading && (
          <span className="animate-pulse text-xs text-gray-400">Loading…</span>
        )}
      </div>

      {/* Calendar grid */}
      <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white">
        <div
          style={{ maxHeight: 'min(800px, calc(100vh - 300px))' }}
          className={`overflow-y-auto${daysToShow === 7 ? ' min-w-[640px]' : ''}`}
        >
          {/* Sticky day headers */}
          <div className="sticky top-0 z-20 flex border-b border-gray-200 bg-gray-50">
            <div className="w-10 shrink-0 md:w-12" />
            {days.map((day) => (
              <div
                key={day.toISOString()}
                className={`flex-1 border-l border-gray-200 py-2 text-center ${isToday(day) ? 'bg-teal-50' : ''}`}
              >
                <p className={`text-xs font-medium uppercase tracking-wide ${isToday(day) ? 'text-teal-600' : 'text-gray-400'}`}>
                  {format(day, 'EEE')}
                </p>
                <p className={`mx-auto mt-0.5 flex h-7 w-7 items-center justify-center rounded-full text-sm font-bold ${
                  isToday(day) ? 'bg-teal-600 text-white' : 'text-gray-800'
                }`}>
                  {format(day, 'd')}
                </p>
              </div>
            ))}
          </div>

          {/* Body */}
          <div className="flex">

            {/* Time axis */}
            <div className="w-10 shrink-0 border-r border-gray-200 bg-white md:w-12">
              {slots.map((label, i) => (
                <div key={label} style={{ height: SLOT_HEIGHT }} className="relative">
                  {i % 2 === 0 && (
                    <span className="absolute right-1 top-1 select-none text-[9px] leading-none text-gray-400 md:text-[10px]">
                      {label}
                    </span>
                  )}
                  <div className={`absolute bottom-0 w-full border-b ${i % 2 === 0 ? 'border-gray-100' : 'border-gray-50'}`} />
                </div>
              ))}
            </div>

            {/* Day columns */}
            {days.map((day) => (
              <div
                key={day.toISOString()}
                className={`relative flex-1 border-l border-gray-200 ${isToday(day) ? 'bg-teal-50/20' : 'bg-white'}`}
                style={{ height: TOTAL_HEIGHT }}
              >
                {/* Grid lines */}
                {slots.map((label, i) => (
                  <div
                    key={label}
                    className={`absolute w-full border-b ${i % 2 === 0 ? 'border-gray-100' : 'border-gray-50'}`}
                    style={{ top: i * SLOT_HEIGHT, height: SLOT_HEIGHT }}
                  />
                ))}

                {/* Current time line */}
                {isToday(day) && nowTop !== null && (
                  <div
                    className="pointer-events-none absolute z-10 w-full"
                    style={{ top: nowTop }}
                  >
                    <div className="absolute -left-1 -top-[3px] h-1.5 w-1.5 rounded-full bg-teal-500" />
                    <div className="h-px w-full bg-teal-400" />
                  </div>
                )}

                {/* Appointment blocks */}
                {layoutDay(apptsByDay(day)).map(({ appt: a, col, totalCols }) => {
                  const dt = new Date(a.appointmentDateTime);
                  const top = topFor(dt);
                  const height = heightFor(a.durationMinutes);
                  if (top < 0 || top >= TOTAL_HEIGHT) return null;
                  const pct = 100 / totalCols;
                  return (
                    <div
                      key={a.id}
                      onClick={() => setSelected(a)}
                      className={`absolute cursor-pointer overflow-hidden rounded border-l-2 px-1 py-0.5 shadow-sm transition hover:brightness-95 hover:shadow-md md:px-1.5 md:py-1 ${STATUS_CLASSES[a.status] ?? ''}`}
                      style={{
                        top,
                        height,
                        left: `calc(${col * pct}% + 2px)`,
                        width: `calc(${pct}% - 4px)`,
                      }}
                    >
                      <p className="truncate text-[10px] font-semibold leading-tight md:text-xs">
                        {a.patientName}
                      </p>
                      <p className="text-[9px] leading-tight opacity-70 md:text-[10px]">
                        {format(dt, 'HH:mm')}
                        {height > 48 && ` · ${a.durationMinutes}m`}
                      </p>
                      {height > 60 && daysToShow <= 3 && (
                        <p className="truncate text-[9px] leading-tight opacity-60">
                          {a.doctorName}
                        </p>
                      )}
                      {height > 56 && daysToShow === 7 && (
                        <p className="truncate text-[10px] leading-tight opacity-60">
                          {a.doctorName}
                        </p>
                      )}
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Appointment detail modal */}
      <Modal open={!!selected} onClose={() => setSelected(null)} title="Appointment Details">
        {selected && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="font-mono text-xs text-gray-400">{selected.referenceNumber}</span>
              <StatusBadge status={selected.status} />
            </div>

            <div className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm">
              {([
                ['Patient',   selected.patientName],
                ['Phone',     selected.patientPhone],
                ['Doctor',    selected.doctorName],
                ['Clinic',    selected.clinicName ?? '—'],
                ['Date / Time', format(new Date(selected.appointmentDateTime), 'MMM d, yyyy HH:mm')],
                ['Duration',  `${selected.durationMinutes} min`],
              ] as [string, string][]).map(([label, value]) => (
                <div key={label}>
                  <p className="text-[10px] font-medium uppercase tracking-wide text-gray-400">{label}</p>
                  <p className="text-gray-800">{value}</p>
                </div>
              ))}
              {selected.notes?.trim() && (
                <div className="col-span-2">
                  <p className="text-[10px] font-medium uppercase tracking-wide text-gray-400">Notes</p>
                  <p className="text-gray-700">{selected.notes}</p>
                </div>
              )}
              {selected.cancellationReason && (
                <div className="col-span-2">
                  <p className="text-[10px] font-medium uppercase tracking-wide text-red-400">Cancellation Reason</p>
                  <p className="text-red-600">{selected.cancellationReason}</p>
                </div>
              )}
            </div>

            <div className="flex flex-wrap gap-2 border-t pt-3">
              {selected.status === 'Pending' && (
                <button
                  disabled={approvingId === selected.id}
                  onClick={() => { onApprove(selected.id); setSelected(null); }}
                  className="rounded bg-green-100 px-3 py-1.5 text-xs font-medium text-green-700 hover:bg-green-200 disabled:opacity-50"
                >
                  {approvingId === selected.id ? 'Approving…' : 'Approve'}
                </button>
              )}
              {selected.status !== 'Cancelled' && (
                <>
                  <button
                    onClick={() => { onCancelClick(selected.id); setSelected(null); }}
                    className="rounded bg-red-100 px-3 py-1.5 text-xs font-medium text-red-700 hover:bg-red-200"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={() => { onRescheduleClick(selected.id); setSelected(null); }}
                    className="rounded bg-blue-100 px-3 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-200"
                  >
                    Reschedule
                  </button>
                </>
              )}
              <button
                onClick={() => setSelected(null)}
                className="ml-auto rounded border px-3 py-1.5 text-xs text-gray-600 hover:bg-gray-50"
              >
                Close
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
