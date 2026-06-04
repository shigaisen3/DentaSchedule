using DentaSchedule.BLL.Common;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.BLL.Interfaces;

public class CreateAppointmentRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
}

public class RescheduleRequest
{
    public DateTime NewDateTime { get; set; }
}

public class CancelRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AppointmentFilter
{
    public AppointmentStatus? Status { get; set; }
    public Guid? ClinicId { get; set; }
    public DateTime? Date { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DashboardStats
{
    public int TotalAppointments { get; set; }
    public int PendingToday { get; set; }
    public int ApprovedToday { get; set; }
    public int CancelledToday { get; set; }
    public int ThisWeek { get; set; }
    public int TotalClinics { get; set; }
    public int TotalDoctors { get; set; }
    public List<RecentAppointment> RecentAppointments { get; set; } = new();
}

public class RecentAppointment
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface IAppointmentService
{
    Task<ServiceResponse<List<TimeSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
    Task<ServiceResponse<Appointment>> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<ServiceResponse<Appointment>> GetAppointmentByReferenceAsync(string referenceNumber);
    Task<ServiceResponse<PagedResult<Appointment>>> GetAppointmentsAsync(AppointmentFilter filter);
    Task<ServiceResponse<Appointment>> ApproveAppointmentAsync(Guid id);
    Task<ServiceResponse<Appointment>> CancelAppointmentAsync(Guid id, string reason);
    Task<ServiceResponse<Appointment>> RescheduleAppointmentAsync(Guid id, DateTime newDateTime);
    Task<ServiceResponse<DashboardStats>> GetDashboardStatsAsync(Guid? clinicId);
}
