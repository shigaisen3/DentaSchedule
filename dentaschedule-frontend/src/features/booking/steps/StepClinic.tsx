import { useEffect, useState } from 'react';
import { clinicApi } from '../../../api/endpoints';
import type { Clinic } from '../../../types';
import type { BookingData } from '../BookingWizard';
import Spinner from '../../../components/ui/Spinner';
import { Building2 } from 'lucide-react';

interface Props {
  data: BookingData;
  update: (d: Partial<BookingData>) => void;
  next: () => void;
}

export default function StepClinic({ data, update, next }: Props) {
  const [clinics, setClinics] = useState<Clinic[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    clinicApi.getActive().then((r) => setClinics(r.data)).finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <h2 className="mb-4 text-lg font-semibold text-gray-800">Select a Clinic</h2>
      <div className="space-y-3">
        {clinics.map((c) => (
          <button
            key={c.id}
            onClick={() => { update({ clinicId: c.id, clinicName: c.name }); next(); }}
            className={`flex w-full items-center gap-4 rounded-lg border p-4 text-left transition hover:border-teal-500 hover:bg-teal-50 ${
              data.clinicId === c.id ? 'border-teal-600 bg-teal-50' : 'border-gray-200'
            }`}
          >
            {c.logoUrl ? (
              <img src={c.logoUrl} alt={c.name} className="h-12 w-12 shrink-0 rounded-lg object-cover" />
            ) : (
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-teal-100">
                <Building2 size={24} className="text-teal-600" />
              </div>
            )}
            <div>
              <p className="font-medium text-gray-900">{c.name}</p>
              <p className="text-sm text-gray-500">{c.address}</p>
              <p className="text-sm text-gray-500">{c.phone} &middot; {c.email}</p>
            </div>
          </button>
        ))}
        {clinics.length === 0 && <p className="text-center text-gray-500">No clinics available at this time.</p>}
      </div>
    </div>
  );
}
