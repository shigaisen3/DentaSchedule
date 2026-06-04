import { useEffect, useState } from 'react';
import { clinicApi, doctorApi } from '../../../api/endpoints';
import type { Clinic, Doctor, CreateDoctorRequest } from '../../../types';
import { Link } from 'react-router-dom';
import Modal from '../../../components/ui/Modal';
import Spinner from '../../../components/ui/Spinner';
import toast from 'react-hot-toast';
import { Plus, Pencil, Trash2, Calendar } from 'lucide-react';

const empty: CreateDoctorRequest = { fullName: '', specialization: '', clinicId: '' };

type FormErrors = Partial<Record<keyof CreateDoctorRequest, string>>;

function validate(form: CreateDoctorRequest): FormErrors {
  const errors: FormErrors = {};
  if (!form.fullName.trim()) errors.fullName = 'Full name is required.';
  if (!form.specialization.trim()) errors.specialization = 'Specialization is required.';
  if (!form.clinicId) errors.clinicId = 'Please select a clinic.';
  return errors;
}

export default function DoctorsPage() {
  const [clinics, setClinics] = useState<Clinic[]>([]);
  const [selectedClinic, setSelectedClinic] = useState('');
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateDoctorRequest>(empty);
  const [isActive, setIsActive] = useState(true);
  const [errors, setErrors] = useState<FormErrors>({});
  const [touched, setTouched] = useState<Partial<Record<keyof CreateDoctorRequest, boolean>>>({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    clinicApi.getActive().then((r) => {
      setClinics(r.data);
      if (r.data.length > 0) setSelectedClinic(r.data[0].id);
    });
  }, []);

  useEffect(() => {
    if (!selectedClinic) { setLoading(false); return; }
    setLoading(true);
    clinicApi.getDoctors(selectedClinic).then((r) => setDoctors(r.data)).finally(() => setLoading(false));
  }, [selectedClinic]);

  const fetchDoctors = () => {
    if (!selectedClinic) return;
    clinicApi.getDoctors(selectedClinic).then((r) => setDoctors(r.data));
  };

  const openCreate = () => {
    setEditId(null);
    setForm({ fullName: '', specialization: '', clinicId: selectedClinic });
    setIsActive(true); setErrors({}); setTouched({});
    setModalOpen(true);
  };

  const openEdit = (d: Doctor) => {
    setEditId(d.id);
    setForm({ fullName: d.fullName, specialization: d.specialization, bio: d.bio, photoUrl: d.photoUrl, clinicId: d.clinicId });
    setIsActive(d.isActive);
    setErrors({}); setTouched({});
    setModalOpen(true);
  };

  const handleChange = (field: keyof CreateDoctorRequest, value: string) => {
    const next = { ...form, [field]: value };
    setForm(next);
    if (touched[field]) setErrors(validate(next));
  };

  const handleBlur = (field: keyof CreateDoctorRequest) => {
    setTouched((t) => ({ ...t, [field]: true }));
    setErrors(validate(form));
  };

  const handleSave = async () => {
    const allTouched = { fullName: true, specialization: true, clinicId: true };
    setTouched(allTouched);
    const errs = validate(form);
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setSaving(true);
    try {
      if (editId) {
        await doctorApi.update(editId, { ...form, isActive });
        toast.success('Doctor updated');
      } else {
        await doctorApi.create(form);
        toast.success('Doctor created');
      }
      setModalOpen(false);
      fetchDoctors();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors?.[0];
      toast.error(msg || 'Failed to save doctor');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this doctor?')) return;
    try {
      await doctorApi.delete(id);
      toast.success('Doctor deleted');
      fetchDoctors();
    } catch { toast.error('Failed to delete doctor'); }
  };

  const inputClass = (field: keyof CreateDoctorRequest) =>
    `w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-1 ${
      touched[field] && errors[field]
        ? 'border-red-400 focus:border-red-400 focus:ring-red-300'
        : 'border-gray-300 focus:border-teal-500 focus:ring-teal-500'
    }`;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">Doctors</h1>
        <button onClick={openCreate}
          className="flex items-center gap-1 rounded-lg bg-teal-600 px-4 py-2 text-sm font-medium text-white hover:bg-teal-700">
          <Plus size={16} /> Add Doctor
        </button>
      </div>

      <select value={selectedClinic} onChange={(e) => setSelectedClinic(e.target.value)}
        className="mb-4 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none">
        {clinics.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>

      {loading ? <Spinner /> : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {doctors.map((d) => {
            const initials = d.fullName.replace('Dr. ', '').split(' ').map((w) => w[0]).slice(0, 2).join('');
            return (
              <div key={d.id} className="rounded-lg border border-gray-200 bg-white p-4">
                <div className="mb-2 flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    {d.photoUrl ? (
                      <img src={d.photoUrl} alt={d.fullName} className="h-10 w-10 rounded-full object-cover" />
                    ) : (
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-teal-600 text-sm font-bold text-white">
                        {initials}
                      </div>
                    )}
                    <div>
                      <h3 className="font-semibold text-gray-900">{d.fullName}</h3>
                      <p className="text-sm text-teal-600">{d.specialization}</p>
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <Link to={`/admin/doctors/${d.id}/schedule`}
                      className="rounded p-1 text-gray-400 hover:bg-teal-50 hover:text-teal-600">
                      <Calendar size={16} />
                    </Link>
                    <button onClick={() => openEdit(d)} className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600">
                      <Pencil size={16} />
                    </button>
                    <button onClick={() => handleDelete(d.id)} className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-500">
                      <Trash2 size={16} />
                    </button>
                  </div>
                </div>
                {d.bio && <p className="text-xs text-gray-400">{d.bio}</p>}
              </div>
            );
          })}
          {doctors.length === 0 && <p className="text-gray-500">No doctors in this clinic.</p>}
        </div>
      )}

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editId ? 'Edit Doctor' : 'New Doctor'}>
        <div className="space-y-3">
          <Field label="Full Name *" error={touched.fullName ? errors.fullName : undefined}>
            <input value={form.fullName} onChange={(e) => handleChange('fullName', e.target.value)}
              onBlur={() => handleBlur('fullName')} className={inputClass('fullName')} placeholder="Dr. First Last" />
          </Field>
          <Field label="Specialization *" error={touched.specialization ? errors.specialization : undefined}>
            <input value={form.specialization} onChange={(e) => handleChange('specialization', e.target.value)}
              onBlur={() => handleBlur('specialization')} className={inputClass('specialization')} placeholder="e.g. Ortodonție" />
          </Field>
          <Field label="Bio">
            <textarea value={form.bio ?? ''} onChange={(e) => setForm({ ...form, bio: e.target.value })}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none focus:ring-1 focus:ring-teal-500"
              rows={2} placeholder="Short bio (optional)" />
          </Field>
          <Field label="Photo URL">
            <input value={form.photoUrl ?? ''} onChange={(e) => setForm({ ...form, photoUrl: e.target.value })}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none focus:ring-1 focus:ring-teal-500"
              placeholder="https://... (optional)" />
          </Field>
          <Field label="Clinic *" error={touched.clinicId ? errors.clinicId : undefined}>
            <select value={form.clinicId} onChange={(e) => handleChange('clinicId', e.target.value)}
              onBlur={() => handleBlur('clinicId')} className={inputClass('clinicId')}>
              <option value="">Select Clinic</option>
              {clinics.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </Field>
          {editId && (
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /> Active
            </label>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <button onClick={() => setModalOpen(false)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleSave} disabled={saving}
              className="rounded-lg bg-teal-600 px-4 py-2 text-sm text-white hover:bg-teal-700 disabled:opacity-50">
              {saving ? 'Saving...' : 'Save'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  const [text, asterisk] = label.endsWith('*') ? [label.slice(0, -1).trimEnd(), true] : [label, false];
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-600">
        {text}{asterisk && <span className="text-red-500"> *</span>}
      </label>
      {children}
      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  );
}
