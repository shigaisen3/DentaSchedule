using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;

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

/// <summary>
/// Wraps a real Unit of Work but throws a supplied exception from <c>SaveChangesAsync</c>.
/// Used to simulate database concurrency failures (e.g. an optimistic-concurrency
/// <c>DbUpdateConcurrencyException</c>) without needing a real SQL Server.
/// </summary>
public class ThrowingUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private readonly Exception _onSave;

    public ThrowingUnitOfWork(IUnitOfWork inner, Exception onSave)
    {
        _inner = inner;
        _onSave = onSave;
    }

    public IRepository<Clinic> Clinics => _inner.Clinics;
    public IRepository<Doctor> Doctors => _inner.Doctors;
    public IRepository<DoctorSchedule> DoctorSchedules => _inner.DoctorSchedules;
    public IRepository<ScheduleException> ScheduleExceptions => _inner.ScheduleExceptions;
    public IRepository<Appointment> Appointments => _inner.Appointments;
    public IRepository<RefreshToken> RefreshTokens => _inner.RefreshTokens;

    public Task<int> SaveChangesAsync() => throw _onSave;

    public void Dispose() => _inner.Dispose();
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
