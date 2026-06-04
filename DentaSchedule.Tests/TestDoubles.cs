using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.Tests;

/// <summary>Records which appointment lifecycle notifications were requested, for assertions.</summary>
public class RecordingNotificationService : IAppointmentNotificationService
{
    public List<Appointment> Created { get; } = new();
    public List<Appointment> Approved { get; } = new();
    public List<Appointment> Cancelled { get; } = new();
    public List<Appointment> Rescheduled { get; } = new();

    public Task NotifyCreatedAsync(Appointment appointment) { Created.Add(appointment); return Task.CompletedTask; }
    public Task NotifyApprovedAsync(Appointment appointment) { Approved.Add(appointment); return Task.CompletedTask; }
    public Task NotifyCancelledAsync(Appointment appointment) { Cancelled.Add(appointment); return Task.CompletedTask; }
    public Task NotifyRescheduledAsync(Appointment appointment) { Rescheduled.Add(appointment); return Task.CompletedTask; }
}

/// <summary>Captures the emails an <see cref="IEmailService"/> is asked to send.</summary>
public class CapturingEmailService : IEmailService
{
    public List<EmailMessage> Sent { get; } = new();

    public Task<bool> SendAsync(EmailMessage message)
    {
        Sent.Add(message);
        return Task.FromResult(true);
    }
}
