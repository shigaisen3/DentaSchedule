import { useEffect, useState } from 'react';
import { clinicApi } from '../../../api/endpoints';
import type { Doctor } from '../../../types';
import type { BookingData } from '../BookingWizard';
import Spinner from '../../../components/ui/Spinner';

interface Props {
  data: BookingData;
  update: (d: Partial<BookingData>) => void;
  next: () => void;
  back: () => void;
}

export default function StepDoctor({ data, update, next, back }: Props) {
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    clinicApi.getDoctors(data.clinicId).then((r) => setDoctors(r.data)).finally(() => setLoading(false));
  }, [data.clinicId]);

  if (loading) return <Spinner />;

  return (
    <div>
      <h2 className="mb-4 text-lg font-semibold text-gray-800">Select a Doctor</h2>
      <div className="space-y-3">
        {doctors.map((d) => {
          const initials = d.fullName.replace('Dr. ', '').split(' ').map((w) => w[0]).slice(0, 2).join('');
          return (
            <button
              key={d.id}
              onClick={() => {
                update({ doctorId: d.id, doctorName: d.fullName, specialization: d.specialization });
                next();
              }}
              className={`flex w-full items-center gap-4 rounded-lg border p-4 text-left transition hover:border-teal-500 hover:bg-teal-50 ${
                data.doctorId === d.id ? 'border-teal-600 bg-teal-50' : 'border-gray-200'
              }`}
            >
              {d.photoUrl ? (
                <img src={d.photoUrl} alt={d.fullName} className="h-12 w-12 shrink-0 rounded-full object-cover" />
              ) : (
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-teal-600 text-sm font-bold text-white">
                  {initials}
                </div>
              )}
              <div>
                <p className="font-medium text-gray-900">{d.fullName}</p>
                <p className="text-sm text-teal-600">{d.specialization}</p>
                {d.bio && <p className="mt-1 text-xs text-gray-400">{d.bio}</p>}
              </div>
            </button>
          );
        })}
        {doctors.length === 0 && <p className="text-center text-gray-500">No doctors available at this clinic.</p>}
      </div>
      <button onClick={back} className="mt-4 text-sm text-gray-500 hover:text-gray-700">&larr; Back</button>
    </div>
  );
}
