import { useState } from 'react';
import { Link } from 'react-router-dom';
import StepClinic from './steps/StepClinic';
import StepDoctor from './steps/StepDoctor';
import StepSlot from './steps/StepSlot';
import StepPatient from './steps/StepPatient';
import StepConfirm from './steps/StepConfirm';
import StepSuccess from './steps/StepSuccess';

export interface BookingData {
  clinicId: string;
  clinicName: string;
  doctorId: string;
  doctorName: string;
  specialization: string;
  date: string;
  time: string;
  durationMinutes: number;
  patientName: string;
  patientEmail: string;
  patientPhone: string;
  notes: string;
}

const STEP_LABELS = ['Clinic', 'Doctor', 'Date & Time', 'Your Details', 'Confirm'];

export default function BookingWizard() {
  const [step, setStep] = useState(0);
  const [referenceNumber, setReferenceNumber] = useState('');
  const [data, setData] = useState<BookingData>({
    clinicId: '', clinicName: '', doctorId: '', doctorName: '', specialization: '',
    date: '', time: '', durationMinutes: 30,
    patientName: '', patientEmail: '', patientPhone: '', notes: '',
  });

  const update = (partial: Partial<BookingData>) => setData((d) => ({ ...d, ...partial }));
  const next = () => setStep((s) => s + 1);
  const back = () => setStep((s) => s - 1);
  const reset = () => {
    setStep(0);
    setReferenceNumber('');
    setData({
      clinicId: '', clinicName: '', doctorId: '', doctorName: '', specialization: '',
      date: '', time: '', durationMinutes: 30,
      patientName: '', patientEmail: '', patientPhone: '', notes: '',
    });
  };

  // Success state (step 5)
  if (step === 5) return <StepSuccess referenceNumber={referenceNumber} onReset={reset} />;

  return (
    <div className="mx-auto min-h-screen max-w-2xl bg-white px-4 py-8">
      <div className="mb-8 text-center">
        <h1 className="text-2xl font-bold text-teal-700">Book an Appointment</h1>
        <p className="text-sm text-gray-500">DentaSchedule - Online Booking</p>
      </div>

      {/* Stepper */}
      <div className="mb-8 flex justify-center gap-2">
        {STEP_LABELS.map((label, i) => (
          <div key={label} className="flex items-center gap-2">
            <div className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold ${
              i <= step ? 'bg-teal-600 text-white' : 'bg-gray-200 text-gray-500'
            }`}>
              {i + 1}
            </div>
            <span className={`hidden text-xs sm:block ${i <= step ? 'text-teal-700 font-medium' : 'text-gray-400'}`}>
              {label}
            </span>
            {i < STEP_LABELS.length - 1 && <div className="mx-1 h-px w-6 bg-gray-300" />}
          </div>
        ))}
      </div>

      {/* Steps */}
      {step === 0 && <StepClinic data={data} update={update} next={next} />}
      {step === 1 && <StepDoctor data={data} update={update} next={next} back={back} />}
      {step === 2 && <StepSlot data={data} update={update} next={next} back={back} />}
      {step === 3 && <StepPatient data={data} update={update} next={next} back={back} />}
      {step === 4 && (
        <StepConfirm data={data} back={back} onSuccess={(ref) => { setReferenceNumber(ref); setStep(5); }} />
      )}

      <div className="mt-10 flex justify-center gap-4 text-sm text-gray-500">
        <Link to="/check" className="hover:text-teal-600">Check Appointment Status</Link>
        <span>·</span>
        <Link to="/login" className="hover:text-teal-600">Staff Login</Link>
      </div>
    </div>
  );
}
