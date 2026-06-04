import { useState } from 'react';
import { appointmentApi } from '../../../api/endpoints';
import type { BookingData } from '../BookingWizard';
import toast from 'react-hot-toast';

interface Props {
  data: BookingData;
  back: () => void;
  onSuccess: (referenceNumber: string) => void;
}

export default function StepConfirm({ data, back, onSuccess }: Props) {
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      const dateTime = `${data.date}T${data.time}:00`;
      const res = await appointmentApi.create({
        patientName: data.patientName,
        patientEmail: data.patientEmail,
        patientPhone: data.patientPhone,
        doctorId: data.doctorId,
        clinicId: data.clinicId,
        appointmentDateTime: dateTime,
        durationMinutes: data.durationMinutes,
        notes: data.notes || undefined,
      });
      toast.success('Appointment booked!');
      onSuccess(res.data.referenceNumber);
    } catch {
      toast.error('Failed to book appointment. The slot may no longer be available.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div>
      <h2 className="mb-4 text-lg font-semibold text-gray-800">Confirm Your Appointment</h2>

      <div className="space-y-3 rounded-lg border border-gray-200 bg-gray-50 p-4">
        <Row label="Clinic" value={data.clinicName} />
        <Row label="Doctor" value={`${data.doctorName} — ${data.specialization}`} />
        <Row label="Date" value={data.date} />
        <Row label="Time" value={data.time} />
        <Row label="Duration" value={`${data.durationMinutes} minutes`} />
        <hr className="border-gray-200" />
        <Row label="Name" value={data.patientName} />
        <Row label="Email" value={data.patientEmail} />
        <Row label="Phone" value={data.patientPhone} />
        {data.notes && <Row label="Notes" value={data.notes} />}
      </div>

      <div className="mt-6 flex justify-between">
        <button onClick={back} className="text-sm text-gray-500 hover:text-gray-700">&larr; Back</button>
        <button
          onClick={handleSubmit}
          disabled={submitting}
          className="rounded-lg bg-teal-600 px-8 py-2.5 text-sm font-medium text-white hover:bg-teal-700 disabled:opacity-50"
        >
          {submitting ? 'Booking...' : 'Confirm Booking'}
        </button>
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between text-sm">
      <span className="text-gray-500">{label}</span>
      <span className="font-medium text-gray-900">{value}</span>
    </div>
  );
}
