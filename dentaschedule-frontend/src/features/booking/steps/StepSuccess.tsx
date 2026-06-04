import { CheckCircle } from 'lucide-react';
import { Link } from 'react-router-dom';

interface Props {
  referenceNumber: string;
  onReset: () => void;
}

export default function StepSuccess({ referenceNumber, onReset }: Props) {
  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col items-center justify-center px-4 text-center">
      <CheckCircle size={64} className="mb-4 text-teal-600" />
      <h1 className="text-2xl font-bold text-gray-900">Appointment Booked!</h1>
      <p className="mt-2 text-gray-600">Your appointment has been submitted and is pending confirmation.</p>

      <div className="mt-6 rounded-lg border border-teal-200 bg-teal-50 px-6 py-4">
        <p className="text-sm text-teal-700">Your Reference Number</p>
        <p className="text-2xl font-bold text-teal-800">{referenceNumber}</p>
      </div>

      <p className="mt-4 text-xs text-gray-500">
        Save this reference number to check your appointment status later.
      </p>

      <div className="mt-6 flex flex-col items-center gap-3">
        <Link
          to={`/check?ref=${referenceNumber}`}
          className="rounded-lg border border-teal-600 px-6 py-2 text-sm font-medium text-teal-600 hover:bg-teal-50"
        >
          Check Appointment Status
        </Link>
        <button
          onClick={onReset}
          className="rounded-lg bg-teal-600 px-6 py-2 text-sm font-medium text-white hover:bg-teal-700"
        >
          Book Another Appointment
        </button>
      </div>
    </div>
  );
}
