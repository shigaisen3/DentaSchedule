import { useEffect, useState } from 'react';
import { clinicApi } from '../../../api/endpoints';
import type { Clinic, CreateClinicRequest } from '../../../types';
import Modal from '../../../components/ui/Modal';
import Spinner from '../../../components/ui/Spinner';
import toast from 'react-hot-toast';
import { Plus, Pencil, Trash2, Building2 } from 'lucide-react';

const empty: CreateClinicRequest = { name: '', address: '', phone: '', email: '' };

type FormErrors = Partial<Record<keyof CreateClinicRequest, string>>;

function validate(form: CreateClinicRequest): FormErrors {
  const errors: FormErrors = {};
  if (!form.name.trim()) errors.name = 'Name is required.';
  if (!form.address.trim()) errors.address = 'Address is required.';
  if (!form.phone.trim()) errors.phone = 'Phone is required.';
  if (!form.email.trim()) errors.email = 'Email is required.';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errors.email = 'Invalid email address.';
  return errors;
}

export default function ClinicsPage() {
  const [clinics, setClinics] = useState<Clinic[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState(empty);
  const [isActive, setIsActive] = useState(true);
  const [errors, setErrors] = useState<FormErrors>({});
  const [touched, setTouched] = useState<Partial<Record<keyof CreateClinicRequest, boolean>>>({});
  const [saving, setSaving] = useState(false);

  const fetchClinics = async () => {
    setLoading(true);
    try {
      const res = await clinicApi.getActive();
      setClinics(res.data);
    } catch { toast.error('Failed to load clinics'); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchClinics(); }, []);

  const openCreate = () => {
    setEditId(null); setForm(empty); setIsActive(true);
    setErrors({}); setTouched({});
    setModalOpen(true);
  };

  const openEdit = (c: Clinic) => {
    setEditId(c.id);
    setForm({ name: c.name, address: c.address, phone: c.phone, email: c.email, logoUrl: c.logoUrl });
    setIsActive(c.isActive);
    setErrors({}); setTouched({});
    setModalOpen(true);
  };

  const handleChange = (field: keyof CreateClinicRequest, value: string) => {
    const next = { ...form, [field]: value };
    setForm(next);
    if (touched[field]) setErrors(validate(next));
  };

  const handleBlur = (field: keyof CreateClinicRequest) => {
    setTouched((t) => ({ ...t, [field]: true }));
    setErrors(validate(form));
  };

  const handleSave = async () => {
    const allTouched = { name: true, address: true, phone: true, email: true };
    setTouched(allTouched);
    const errs = validate(form);
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setSaving(true);
    try {
      if (editId) {
        await clinicApi.update(editId, { ...form, isActive });
        toast.success('Clinic updated');
      } else {
        await clinicApi.create(form);
        toast.success('Clinic created');
      }
      setModalOpen(false);
      fetchClinics();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors?.[0];
      toast.error(msg || 'Failed to save clinic');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this clinic?')) return;
    try {
      await clinicApi.delete(id);
      toast.success('Clinic deleted');
      fetchClinics();
    } catch { toast.error('Failed to delete clinic'); }
  };

  const inputClass = (field: keyof CreateClinicRequest) =>
    `w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-1 ${
      touched[field] && errors[field]
        ? 'border-red-400 focus:border-red-400 focus:ring-red-300'
        : 'border-gray-300 focus:border-teal-500 focus:ring-teal-500'
    }`;

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">Clinics</h1>
        <button onClick={openCreate}
          className="flex items-center gap-1 rounded-lg bg-teal-600 px-4 py-2 text-sm font-medium text-white hover:bg-teal-700">
          <Plus size={16} /> Add Clinic
        </button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {clinics.map((c) => (
          <div key={c.id} className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="mb-3 flex items-start justify-between">
              <div className="flex items-center gap-3">
                {c.logoUrl ? (
                  <img src={c.logoUrl} alt={c.name} className="h-10 w-10 rounded-lg object-cover" />
                ) : (
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-teal-100">
                    <Building2 size={20} className="text-teal-600" />
                  </div>
                )}
                <div>
                  <h3 className="font-semibold text-gray-900">{c.name}</h3>
                  <span className={`text-xs font-medium ${c.isActive ? 'text-green-600' : 'text-red-500'}`}>
                    {c.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </div>
              <div className="flex gap-1">
                <button onClick={() => openEdit(c)} className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600">
                  <Pencil size={16} />
                </button>
                <button onClick={() => handleDelete(c.id)} className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-500">
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
            <p className="text-sm text-gray-500">{c.address}</p>
            <p className="text-sm text-gray-500">{c.phone} &middot; {c.email}</p>
          </div>
        ))}
      </div>

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editId ? 'Edit Clinic' : 'New Clinic'}>
        <div className="space-y-3">
          <Field label="Name *" error={touched.name ? errors.name : undefined}>
            <input value={form.name} onChange={(e) => handleChange('name', e.target.value)}
              onBlur={() => handleBlur('name')} className={inputClass('name')} placeholder="Clinic name" />
          </Field>
          <Field label="Address *" error={touched.address ? errors.address : undefined}>
            <input value={form.address} onChange={(e) => handleChange('address', e.target.value)}
              onBlur={() => handleBlur('address')} className={inputClass('address')} placeholder="Street, City" />
          </Field>
          <Field label="Phone *" error={touched.phone ? errors.phone : undefined}>
            <input value={form.phone} onChange={(e) => handleChange('phone', e.target.value)}
              onBlur={() => handleBlur('phone')} className={inputClass('phone')} placeholder="07xx xxx xxx" />
          </Field>
          <Field label="Email *" error={touched.email ? errors.email : undefined}>
            <input type="email" value={form.email} onChange={(e) => handleChange('email', e.target.value)}
              onBlur={() => handleBlur('email')} className={inputClass('email')} placeholder="contact@clinic.ro" />
          </Field>
          <Field label="Logo URL">
            <input value={form.logoUrl ?? ''} onChange={(e) => setForm({ ...form, logoUrl: e.target.value })}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-teal-500 focus:outline-none focus:ring-1 focus:ring-teal-500"
              placeholder="https://... (optional)" />
          </Field>
          {editId && (
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              Active
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
