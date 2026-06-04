using DentaSchedule.BLL.Interfaces;
using DentaSchedule.BLL.Services;
using DentaSchedule.DAL.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentaSchedule.Tests;

/// <summary>
/// Verifies that <c>AppointmentService</c> fires the right notification for each lifecycle
/// event (and none on failure), and that <c>AppointmentNotificationService</c> composes a
/// correct patient email.
/// </summary>
public class NotificationTests
{
    // ── Service fires the correct notification ──

    [Fact]
    public async Task Create_FiresCreatedNotification()
    {
        using var ctx = new AppointmentTestContext();

        await ctx.Service.CreateAppointmentAsync(new CreateAppointmentRequest
        {
            PatientName = "Ana",
            PatientEmail = "ana@test.ro",
            PatientPhone = "0700000003",
            DoctorId = AppointmentTestContext.DoctorId,
            ClinicId = AppointmentTestContext.ClinicId,
            AppointmentDateTime = AppointmentTestContext.TestDate.AddHours(10),
            DurationMinutes = 30
        });

        Assert.Single(ctx.Notifications.Created);
        Assert.Empty(ctx.Notifications.Approved);
    }

    [Fact]
    public async Task Approve_FiresApprovedNotification()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        await ctx.Service.ApproveAppointmentAsync(id);

        Assert.Single(ctx.Notifications.Approved);
    }

    [Fact]
    public async Task Cancel_FiresCancelledNotification()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        await ctx.Service.CancelAppointmentAsync(id, "Patient request");

        Assert.Single(ctx.Notifications.Cancelled);
    }

    [Fact]
    public async Task Reschedule_FiresRescheduledNotification()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        await ctx.Service.RescheduleAppointmentAsync(id, AppointmentTestContext.TestDate.AddHours(14));

        Assert.Single(ctx.Notifications.Rescheduled);
    }

    [Fact]
    public async Task FailedOperation_FiresNoNotification()
    {
        using var ctx = new AppointmentTestContext();

        // Past date → create fails before any save.
        await ctx.Service.CreateAppointmentAsync(new CreateAppointmentRequest
        {
            PatientName = "Ana",
            PatientEmail = "ana@test.ro",
            PatientPhone = "0700000003",
            DoctorId = AppointmentTestContext.DoctorId,
            ClinicId = AppointmentTestContext.ClinicId,
            AppointmentDateTime = DateTime.Now.AddDays(-1),
            DurationMinutes = 30
        });

        Assert.Empty(ctx.Notifications.Created);
    }

    // ── Notification service composes a real email ──

    [Fact]
    public async Task NotificationService_ComposesEmailToPatient_WithReferenceAndNames()
    {
        using var ctx = new AppointmentTestContext();
        var email = new CapturingEmailService();
        var notifier = new AppointmentNotificationService(
            email, ctx.UnitOfWork, NullLogger<AppointmentNotificationService>.Instance);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientName = "Ana Pop",
            PatientEmail = "ana@test.ro",
            PatientPhone = "0700000004",
            DoctorId = AppointmentTestContext.DoctorId,
            ClinicId = AppointmentTestContext.ClinicId,
            AppointmentDateTime = AppointmentTestContext.TestDate.AddHours(10),
            DurationMinutes = 30,
            ReferenceNumber = "DS-12345678",
            Status = AppointmentStatus.Pending
        };

        await notifier.NotifyCreatedAsync(appointment);

        var sent = Assert.Single(email.Sent);
        Assert.Equal("ana@test.ro", sent.ToEmail);
        Assert.Contains("DS-12345678", sent.Subject);
        Assert.Contains("DS-12345678", sent.PlainBody);
        Assert.Contains("Dr. Test", sent.PlainBody);      // doctor name resolved via UnitOfWork
        Assert.Contains("Test Clinic", sent.PlainBody);   // clinic name resolved via UnitOfWork
    }
}
