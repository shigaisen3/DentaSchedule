import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { BookingData } from '../BookingWizard';

const schema = z.object({
  patientName: z.string().min(2, 'Name is required'),
  patientEmail: z.string().email('Invalid email'),
  patientPhone: z.string().min(5, 'Phone is required'),
  notes: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  data: BookingData;
  update: (d: Partial<BookingData>) => void;
  next: () => void;
  back: () => void;
}

export default function StepPatient({ data, update, next, back }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      patientName: data.patientName,
      patientEmail: data.patientEmail,
      patientPhone: data.patientPhone,
      notes: data.notes,
    },
  });

  const onSubmit = (form: FormData) => {
    update(form);
    next();
  };

  const inputClass = 'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:ring-1 focus:ring-teal-500 focus:outline-none';

  return (
    <div>
      <h2 className="mb-4 text-lg font-semibold text-gray-800">Your Details</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Full Name <span className="text-red-500">*</span></label>
          <input {...register('patientName')} className={inputClass} />
          {errors.patientName && <p className="mt-1 text-xs text-red-500">{errors.patientName.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Email <span className="text-red-500">*</span></label>
          <input type="email" {...register('patientEmail')} className={inputClass} />
          {errors.patientEmail && <p className="mt-1 text-xs text-red-500">{errors.patientEmail.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Phone <span className="text-red-500">*</span></label>
          <input type="tel" {...register('patientPhone')} className={inputClass} />
          {errors.patientPhone && <p className="mt-1 text-xs text-red-500">{errors.patientPhone.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Notes (optional)</label>
          <textarea {...register('notes')} rows={3} className={inputClass} />
        </div>

        <div className="flex justify-between">
          <button type="button" onClick={back} className="text-sm text-gray-500 hover:text-gray-700">&larr; Back</button>
          <button type="submit" className="rounded-lg bg-teal-600 px-6 py-2 text-sm font-medium text-white hover:bg-teal-700">
            Continue &rarr;
          </button>
        </div>
      </form>
    </div>
  );
}
