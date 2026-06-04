using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using Xunit;

namespace DentaSchedule.Tests;

/// <summary>
/// Covers the appointment lifecycle on <c>AppointmentService</c>:
/// creation (with conflict / working-hours validation), approval, cancellation,
/// and rescheduling.
/// </summary>
public class AppointmentLifecycleTests
{
    private static CreateAppointmentRequest ValidRequest(DateTime when, int duration = 30) => new()
    {
        PatientName = "Ion Popescu",
        PatientEmail = "ion@test.ro",
        PatientPhone = "0700000002",
        DoctorId = AppointmentTestContext.DoctorId,
        ClinicId = AppointmentTestContext.ClinicId,
        AppointmentDateTime = when,
        DurationMinutes = duration
    };

    // ── Create ──

    [Fact]
    public async Task Create_Succeeds_AndStartsPendingWithReference()
    {
        using var ctx = new AppointmentTestContext();

        var result = await ctx.Service.CreateAppointmentAsync(
            ValidRequest(AppointmentTestContext.TestDate.AddHours(10)));

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Pending, result.Data!.Status);
        Assert.StartsWith("DS-", result.Data!.ReferenceNumber);
    }

    [Fact]
    public async Task Create_Fails_WhenInThePast()
    {
        using var ctx = new AppointmentTestContext();

        var result = await ctx.Service.CreateAppointmentAsync(
            ValidRequest(DateTime.Now.AddDays(-1)));

        Assert.False(result.Success);
        Assert.Contains("Cannot create an appointment in the past.", result.Errors);
    }

    [Fact]
    public async Task Create_Fails_WhenDoctorNotAtClinic()
    {
        using var ctx = new AppointmentTestContext();
        var request = ValidRequest(AppointmentTestContext.TestDate.AddHours(10));
        request.ClinicId = Guid.NewGuid(); // doctor does not belong here

        var result = await ctx.Service.CreateAppointmentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Doctor not found or not available at this clinic.", result.Errors);
    }

    [Fact]
    public async Task Create_Fails_WhenOutsideWorkingHours()
    {
        using var ctx = new AppointmentTestContext();

        // 18:00 is past the 09:00–17:00 schedule.
        var result = await ctx.Service.CreateAppointmentAsync(
            ValidRequest(AppointmentTestContext.TestDate.AddHours(18)));

        Assert.False(result.Success);
        Assert.Contains("The selected time is outside the doctor's working hours.", result.Errors);
    }

    [Fact]
    public async Task Create_Fails_WhenConflictingWithApprovedAppointment()
    {
        using var ctx = new AppointmentTestContext();
        ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var result = await ctx.Service.CreateAppointmentAsync(
            ValidRequest(AppointmentTestContext.TestDate.AddHours(10)));

        Assert.False(result.Success);
        Assert.Contains("The selected time slot is no longer available.", result.Errors);
    }

    // ── Approve ──

    [Fact]
    public async Task Approve_Succeeds_ForPendingAppointment()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        var result = await ctx.Service.ApproveAppointmentAsync(id);

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Approved, result.Data!.Status);
    }

    [Fact]
    public async Task Approve_Fails_WhenNotPending()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var result = await ctx.Service.ApproveAppointmentAsync(id);

        Assert.False(result.Success);
        Assert.Contains("Only pending appointments can be approved.", result.Errors);
    }

    // ── Cancel ──

    [Fact]
    public async Task Cancel_Succeeds_AndStoresReason()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var result = await ctx.Service.CancelAppointmentAsync(id, "Patient request");

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Cancelled, result.Data!.Status);
        Assert.Equal("Patient request", result.Data!.CancellationReason);
    }

    [Fact]
    public async Task Cancel_Fails_WhenAlreadyCancelled()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Cancelled);

        var result = await ctx.Service.CancelAppointmentAsync(id, "Again");

        Assert.False(result.Success);
        Assert.Contains("Appointment is already cancelled.", result.Errors);
    }

    // ── Reschedule ──

    [Fact]
    public async Task Reschedule_Succeeds_AndResetsToPending()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);
        var newTime = AppointmentTestContext.TestDate.AddHours(14);

        var result = await ctx.Service.RescheduleAppointmentAsync(id, newTime);

        Assert.True(result.Success);
        Assert.Equal(newTime, result.Data!.AppointmentDateTime);
        Assert.Equal(AppointmentStatus.Pending, result.Data!.Status);
    }

    [Fact]
    public async Task Reschedule_Fails_WhenNewTimeIsInThePast()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var result = await ctx.Service.RescheduleAppointmentAsync(id, DateTime.Now.AddDays(-1));

        Assert.False(result.Success);
        Assert.Contains("Cannot reschedule to a past date and time.", result.Errors);
    }

    [Fact]
    public async Task Reschedule_Fails_WhenAppointmentIsCancelled()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Cancelled);

        var result = await ctx.Service.RescheduleAppointmentAsync(
            id, AppointmentTestContext.TestDate.AddHours(14));

        Assert.False(result.Success);
        Assert.Contains("Cannot reschedule a cancelled appointment.", result.Errors);
    }
}
