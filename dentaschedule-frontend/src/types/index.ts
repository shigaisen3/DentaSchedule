// ── Auth ──
export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  displayName: string;
  email: string;
  roles: string[];
  clinicName?: string;
}

export interface AuthUser {
  accessToken: string;
  displayName: string;
  email: string;
  roles: string[];
  clinicName?: string;
}

// ── Clinic ──
export interface Clinic {
  id: string;
  name: string;
  address: string;
  phone: string;
  email: string;
  logoUrl?: string;
  isActive: boolean;
}

export interface CreateClinicRequest {
  name: string;
  address: string;
  phone: string;
  email: string;
  logoUrl?: string;
}

export interface UpdateClinicRequest extends CreateClinicRequest {
  isActive: boolean;
}

// ── Doctor ──
export interface Doctor {
  id: string;
  fullName: string;
  specialization: string;
  bio?: string;
  photoUrl?: string;
  isActive: boolean;
  clinicId: string;
  clinicName?: string;
}

export interface CreateDoctorRequest {
  fullName: string;
  specialization: string;
  bio?: string;
  photoUrl?: string;
  clinicId: string;
}

export interface UpdateDoctorRequest extends CreateDoctorRequest {
  isActive: boolean;
}

// ── Doctor Schedule ──
export interface DoctorSchedule {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface CreateScheduleRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

// ── Schedule Exception ──
export interface ScheduleException {
  id: string;
  exceptionDate: string;
  reason?: string;
  isFullDayOff: boolean;
  customStart?: string;
  customEnd?: string;
}

export interface CreateExceptionRequest {
  exceptionDate: string;
  reason?: string;
  isFullDayOff: boolean;
  customStart?: string;
  customEnd?: string;
}

// ── Appointment ──
export interface Appointment {
  id: string;
  patientName: string;
  patientEmail: string;
  patientPhone: string;
  appointmentDateTime: string;
  durationMinutes: number;
  status: string;
  notes?: string;
  cancellationReason?: string;
  referenceNumber: string;
  createdAt: string;
  doctorId: string;
  doctorName?: string;
  clinicId: string;
  clinicName?: string;
}

export interface CreateAppointmentRequest {
  patientName: string;
  patientEmail: string;
  patientPhone: string;
  doctorId: string;
  clinicId: string;
  appointmentDateTime: string;
  durationMinutes: number;
  notes?: string;
}

export interface RecentAppointment {
  referenceNumber: string;
  patientName: string;
  doctorName: string;
  clinicName: string;
  appointmentDateTime: string;
  status: string;
}

export interface DashboardStats {
  totalAppointments: number;
  pendingToday: number;
  approvedToday: number;
  cancelledToday: number;
  thisWeek: number;
  totalClinics: number;
  totalDoctors: number;
  recentAppointments: RecentAppointment[];
}

export interface TimeSlot {
  time: string;
  isAvailable: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── User ──
export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  clinicId?: string;
  isLockedOut: boolean;
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
  role: string;
  clinicId?: string;
}
