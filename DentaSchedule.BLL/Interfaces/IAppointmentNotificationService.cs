using DentaSchedule.DAL.Entities;

namespace DentaSchedule.BLL.Interfaces;

/// <summary>
/// Composes and sends patient-facing notifications for appointment lifecycle events.
/// Implementations must never throw — failures are logged and swallowed.
/// </summary>
public interface IAppointmentNotificationService
{
    Task NotifyCreatedAsync(Appointment appointment);
    Task NotifyApprovedAsync(Appointment appointment);
    Task NotifyCancelledAsync(Appointment appointment);
    Task NotifyRescheduledAsync(Appointment appointment);
}
