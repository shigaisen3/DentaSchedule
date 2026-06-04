import client from './client';
import type {
  LoginRequest, AuthResponse, Clinic, CreateClinicRequest, UpdateClinicRequest,
  Doctor, CreateDoctorRequest, UpdateDoctorRequest,
  DoctorSchedule, CreateScheduleRequest,
  ScheduleException, CreateExceptionRequest,
  Appointment, CreateAppointmentRequest, TimeSlot, PagedResult,
  UserResponse, CreateUserRequest, DashboardStats,
} from '../types';

// ── Auth ──
export const authApi = {
  login: (data: LoginRequest) =>
    client.post<AuthResponse>('/auth/login', data),
  refresh: (refreshToken: string) =>
    client.post<AuthResponse>('/auth/refresh', { refreshToken }),
  logout: (refreshToken: string) =>
    client.post('/auth/logout', { refreshToken }),
};

// ── Clinics ──
export const clinicApi = {
  getActive: () =>
    client.get<Clinic[]>('/clinics'),
  getById: (id: string) =>
    client.get<Clinic>(`/clinics/${id}`),
  getDoctors: (clinicId: string) =>
    client.get<Doctor[]>(`/clinics/${clinicId}/doctors`),
  create: (data: CreateClinicRequest) =>
    client.post<Clinic>('/clinics', data),
  update: (id: string, data: UpdateClinicRequest) =>
    client.put<Clinic>(`/clinics/${id}`, data),
  delete: (id: string) =>
    client.delete(`/clinics/${id}`),
};

// ── Doctors ──
export const doctorApi = {
  getById: (id: string) =>
    client.get<Doctor>(`/doctors/${id}`),
  getAvailableSlots: (doctorId: string, date: string) =>
    client.get<TimeSlot[]>(`/doctors/${doctorId}/available-slots`, { params: { date } }),
  create: (data: CreateDoctorRequest) =>
    client.post<Doctor>('/doctors', data),
  update: (id: string, data: UpdateDoctorRequest) =>
    client.put<Doctor>(`/doctors/${id}`, data),
  delete: (id: string) =>
    client.delete(`/doctors/${id}`),
  // Schedule
  getSchedules: (doctorId: string) =>
    client.get<DoctorSchedule[]>(`/doctors/${doctorId}/schedule`),
  createSchedule: (doctorId: string, data: CreateScheduleRequest) =>
    client.post<DoctorSchedule>(`/doctors/${doctorId}/schedule`, data),
  updateSchedule: (doctorId: string, scheduleId: string, data: CreateScheduleRequest) =>
    client.put<DoctorSchedule>(`/doctors/${doctorId}/schedule/${scheduleId}`, data),
  deleteSchedule: (doctorId: string, scheduleId: string) =>
    client.delete(`/doctors/${doctorId}/schedule/${scheduleId}`),
  // Exceptions
  getExceptions: (doctorId: string) =>
    client.get<ScheduleException[]>(`/doctors/${doctorId}/exceptions`),
  createException: (doctorId: string, data: CreateExceptionRequest) =>
    client.post<ScheduleException>(`/doctors/${doctorId}/exceptions`, data),
  deleteException: (doctorId: string, exceptionId: string) =>
    client.delete(`/doctors/${doctorId}/exceptions/${exceptionId}`),
};

// ── Appointments ──
export const appointmentApi = {
  getStats: () =>
    client.get<DashboardStats>('/appointments/dashboard'),
  create: (data: CreateAppointmentRequest) =>
    client.post<Appointment>('/appointments', data),
  getByReference: (reference: string) =>
    client.get<Appointment>(`/appointments/${reference}`),
  getAll: (params: { status?: string; clinicId?: string; date?: string; dateFrom?: string; dateTo?: string; page?: number; pageSize?: number }) =>
    client.get<PagedResult<Appointment>>('/appointments', { params }),
  approve: (id: string) =>
    client.put<Appointment>(`/appointments/${id}/approve`),
  cancel: (id: string, reason: string) =>
    client.put<Appointment>(`/appointments/${id}/cancel`, { reason }),
  reschedule: (id: string, newDateTime: string) =>
    client.put<Appointment>(`/appointments/${id}/reschedule`, { newDateTime }),
};

// ── Users ──
export const userApi = {
  getAll: () =>
    client.get<UserResponse[]>('/users'),
  getById: (id: string) =>
    client.get<UserResponse>(`/users/${id}`),
  create: (data: CreateUserRequest) =>
    client.post<UserResponse>('/users', data),
  addRole: (id: string, role: string) =>
    client.post(`/users/${id}/roles`, { role }),
  removeRole: (id: string, role: string) =>
    client.delete(`/users/${id}/roles/${role}`),
  lockout: (id: string) =>
    client.put(`/users/${id}/lockout`),
  unlock: (id: string) =>
    client.put(`/users/${id}/unlock`),
  resetPassword: (id: string, newPassword: string) =>
    client.post(`/users/${id}/reset-password`, { newPassword }),
  delete: (id: string) =>
    client.delete(`/users/${id}`),
};
