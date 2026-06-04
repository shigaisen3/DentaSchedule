import { useEffect, useState } from 'react';
import { userApi, clinicApi } from '../../../api/endpoints';
import type { UserResponse, Clinic, CreateUserRequest } from '../../../types';
import Modal from '../../../components/ui/Modal';
import Spinner from '../../../components/ui/Spinner';
import toast from 'react-hot-toast';
import { Plus, Lock, Unlock, KeyRound, Trash2 } from 'lucide-react';

const emptyForm: CreateUserRequest = { email: '', displayName: '', password: '', role: 'Assistant' };

type FormErrors = Partial<Record<keyof CreateUserRequest, string>>;
type PasswordErrors = { password?: string };

function validateCreate(form: CreateUserRequest): FormErrors {
  const errors: FormErrors = {};
  if (!form.displayName.trim()) errors.displayName = 'Display name is required.';
  if (!form.email.trim()) errors.email = 'Email is required.';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errors.email = 'Invalid email address.';
  if (!form.password) errors.password = 'Password is required.';
  else if (form.password.length < 8) errors.password = 'Minimum 8 characters.';
  else if (!/[A-Z]/.test(form.password)) errors.password = 'Must contain an uppercase letter.';
  else if (!/[0-9]/.test(form.password)) errors.password = 'Must contain a number.';
  else if (!/[^A-Za-z0-9]/.test(form.password)) errors.password = 'Must contain a special character.';
  return errors;
}

function validatePassword(pw: string): PasswordErrors {
  if (!pw) return { password: 'Password is required.' };
  if (pw.length < 8) return { password: 'Minimum 8 characters.' };
  if (!/[A-Z]/.test(pw)) return { password: 'Must contain an uppercase letter.' };
  if (!/[0-9]/.test(pw)) return { password: 'Must contain a number.' };
  if (!/[^A-Za-z0-9]/.test(pw)) return { password: 'Must contain a special character.' };
  return {};
}

export default function UsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [clinics, setClinics] = useState<Clinic[]>([]);
  const [loading, setLoading] = useState(true);

  // Create modal
  const [createModal, setCreateModal] = useState(false);
  const [form, setForm] = useState<CreateUserRequest>(emptyForm);
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [formTouched, setFormTouched] = useState<Partial<Record<keyof CreateUserRequest, boolean>>>({});
  const [creating, setCreating] = useState(false);

  // Reset password modal
  const [resetModal, setResetModal] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [pwError, setPwError] = useState('');
  const [pwTouched, setPwTouched] = useState(false);
  const [resetting, setResetting] = useState(false);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const [u, c] = await Promise.all([userApi.getAll(), clinicApi.getActive()]);
      setUsers(u.data);
      setClinics(c.data);
    } catch { toast.error('Failed to load users'); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchUsers(); }, []);

  const openCreate = () => {
    setForm(emptyForm); setFormErrors({}); setFormTouched({});
    setCreateModal(true);
  };

  const handleFormChange = (field: keyof CreateUserRequest, value: string) => {
    const next = { ...form, [field]: value };
    setForm(next);
    if (formTouched[field]) setFormErrors(validateCreate(next));
  };

  const handleFormBlur = (field: keyof CreateUserRequest) => {
    setFormTouched((t) => ({ ...t, [field]: true }));
    setFormErrors(validateCreate(form));
  };

  const handleCreate = async () => {
    setFormTouched({ displayName: true, email: true, password: true, role: true });
    const errs = validateCreate(form);
    setFormErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setCreating(true);
    try {
      await userApi.create(form);
      toast.success('User created');
      setCreateModal(false);
      fetchUsers();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors?.[0];
      toast.error(`Failed to create user${msg ? `: ${msg}` : ''}`);
    } finally {
      setCreating(false);
    }
  };

  const handleToggleLock = async (u: UserResponse) => {
    try {
      if (u.isLockedOut) { await userApi.unlock(u.id); toast.success('User unlocked'); }
      else { await userApi.lockout(u.id); toast.success('User locked'); }
      fetchUsers();
    } catch { toast.error('Failed to update lock status'); }
  };

  const handleResetPassword = async () => {
    setPwTouched(true);
    const errs = validatePassword(newPassword);
    setPwError(errs.password ?? '');
    if (errs.password || !resetModal) return;

    setResetting(true);
    try {
      await userApi.resetPassword(resetModal, newPassword);
      toast.success('Password reset');
      setResetModal(null);
      setNewPassword(''); setPwError(''); setPwTouched(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors?.[0];
      toast.error(msg || 'Failed to reset password');
    } finally {
      setResetting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this user?')) return;
    try {
      await userApi.delete(id);
      toast.success('User deleted');
      fetchUsers();
    } catch { toast.error('Failed to delete user'); }
  };

  const fieldClass = (hasError: boolean) =>
    `w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-1 ${
      hasError
        ? 'border-red-400 focus:border-red-400 focus:ring-red-300'
        : 'border-gray-300 focus:border-teal-500 focus:ring-teal-500'
    }`;

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">Users</h1>
        <button onClick={openCreate}
          className="flex items-center gap-1 rounded-lg bg-teal-600 px-4 py-2 text-sm font-medium text-white hover:bg-teal-700">
          <Plus size={16} /> Add User
        </button>
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="w-full text-left text-sm">
          <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
            <tr>
              <th className="px-4 py-3">Name</th>
              <th className="px-4 py-3">Email</th>
              <th className="px-4 py-3">Roles</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-medium">{u.displayName}</td>
                <td className="px-4 py-3 text-gray-500">{u.email}</td>
                <td className="px-4 py-3">
                  {u.roles.map((r) => (
                    <span key={r} className="mr-1 inline-block rounded-full bg-teal-100 px-2 py-0.5 text-xs font-medium text-teal-700">{r}</span>
                  ))}
                </td>
                <td className="px-4 py-3">
                  <span className={`text-xs font-medium ${u.isLockedOut ? 'text-red-500' : 'text-green-600'}`}>
                    {u.isLockedOut ? 'Locked' : 'Active'}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-1">
                    <button onClick={() => handleToggleLock(u)} title={u.isLockedOut ? 'Unlock' : 'Lock'}
                      className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600">
                      {u.isLockedOut ? <Unlock size={16} /> : <Lock size={16} />}
                    </button>
                    <button onClick={() => { setResetModal(u.id); setNewPassword(''); setPwError(''); setPwTouched(false); }}
                      title="Reset Password" className="rounded p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600">
                      <KeyRound size={16} />
                    </button>
                    <button onClick={() => handleDelete(u.id)} title="Delete"
                      className="rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-500">
                      <Trash2 size={16} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create User Modal */}
      <Modal open={createModal} onClose={() => setCreateModal(false)} title="Create User">
        <div className="space-y-3">
          <Field label="Display Name *" error={formTouched.displayName ? formErrors.displayName : undefined}>
            <input value={form.displayName} onChange={(e) => handleFormChange('displayName', e.target.value)}
              onBlur={() => handleFormBlur('displayName')}
              className={fieldClass(!!(formTouched.displayName && formErrors.displayName))}
              placeholder="Full name" />
          </Field>
          <Field label="Email *" error={formTouched.email ? formErrors.email : undefined}>
            <input type="email" value={form.email} onChange={(e) => handleFormChange('email', e.target.value)}
              onBlur={() => handleFormBlur('email')}
              className={fieldClass(!!(formTouched.email && formErrors.email))}
              placeholder="user@example.com" />
          </Field>
          <Field label="Password *" error={formTouched.password ? formErrors.password : undefined}>
            <input type="password" value={form.password} onChange={(e) => handleFormChange('password', e.target.value)}
              onBlur={() => handleFormBlur('password')}
              className={fieldClass(!!(formTouched.password && formErrors.password))}
              placeholder="Min 8 chars, uppercase, number, symbol" />
          </Field>
          <Field label="Role *">
            <select value={form.role} onChange={(e) => handleFormChange('role', e.target.value)}
              className={fieldClass(false)}>
              <option value="Assistant">Assistant</option>
              <option value="Admin">Admin</option>
            </select>
          </Field>
          <Field label="Clinic">
            <select value={form.clinicId ?? ''} onChange={(e) => setForm({ ...form, clinicId: e.target.value || undefined })}
              className={fieldClass(false)}>
              <option value="">No Clinic Assigned</option>
              {clinics.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </Field>
          <div className="flex justify-end gap-2 pt-2">
            <button onClick={() => setCreateModal(false)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleCreate} disabled={creating}
              className="rounded-lg bg-teal-600 px-4 py-2 text-sm text-white hover:bg-teal-700 disabled:opacity-50">
              {creating ? 'Creating...' : 'Create'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Reset Password Modal */}
      <Modal open={!!resetModal} onClose={() => setResetModal(null)} title="Reset Password">
        <div className="space-y-3">
          <Field label="New Password *" error={pwTouched ? pwError : undefined}>
            <input type="password" value={newPassword}
              onChange={(e) => { setNewPassword(e.target.value); if (pwTouched) setPwError(validatePassword(e.target.value).password ?? ''); }}
              onBlur={() => { setPwTouched(true); setPwError(validatePassword(newPassword).password ?? ''); }}
              className={fieldClass(!!(pwTouched && pwError))}
              placeholder="Min 8 chars, uppercase, number, symbol" />
          </Field>
          <div className="flex justify-end gap-2">
            <button onClick={() => setResetModal(null)} className="rounded-lg border px-4 py-2 text-sm">Cancel</button>
            <button onClick={handleResetPassword} disabled={resetting}
              className="rounded-lg bg-teal-600 px-4 py-2 text-sm text-white hover:bg-teal-700 disabled:opacity-50">
              {resetting ? 'Resetting...' : 'Reset'}
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
