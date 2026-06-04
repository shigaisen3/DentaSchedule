using DentaSchedule.BLL.Services;
using DentaSchedule.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentaSchedule.Tests;

/// <summary>
/// Covers the concurrency safeguards added to <c>AppointmentService</c>:
/// the approval-time conflict re-check (prevents double-booking a slot) and graceful
/// handling of optimistic-concurrency failures on the update operations.
/// </summary>
public class ConcurrencyTests
{
    private const string ConcurrencyMessage =
        "This appointment was just modified by someone else. Please reload and try again.";
    private const string SlotAlreadyApprovedMessage =
        "Another appointment has already been approved for this time slot.";

    // ── Approval-time re-check ──

    [Fact]
    public async Task Approve_Fails_WhenSlotAlreadyHasAnApprovedAppointment()
    {
        using var ctx = new AppointmentTestContext();
        // Two pending requests landed on the same 10:00 slot (allowed — pending does not reserve).
        ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);   // already approved
        var secondId = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        var result = await ctx.Service.ApproveAppointmentAsync(secondId);

        Assert.False(result.Success);
        Assert.Contains(SlotAlreadyApprovedMessage, result.Errors);
    }

    [Fact]
    public async Task Approve_Succeeds_WhenNoApprovedConflictExists()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        var result = await ctx.Service.ApproveAppointmentAsync(id);

        Assert.True(result.Success);
        Assert.Equal(AppointmentStatus.Approved, result.Data!.Status);
    }

    // ── Optimistic-concurrency failure handling ──

    [Fact]
    public async Task Approve_ReturnsFriendlyError_OnConcurrencyConflict()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        var service = ServiceThatFailsToSave(ctx, new DbUpdateConcurrencyException("simulated"));
        var result = await service.ApproveAppointmentAsync(id);

        Assert.False(result.Success);
        Assert.Contains(ConcurrencyMessage, result.Errors);
    }

    [Fact]
    public async Task Approve_ReturnsSlotTakenError_OnUniqueIndexViolation()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        // A DbUpdateException (non-concurrency) models the unique-index violation raised
        // when a concurrent request approved the same slot first.
        var service = ServiceThatFailsToSave(ctx, new DbUpdateException("unique violation"));
        var result = await service.ApproveAppointmentAsync(id);

        Assert.False(result.Success);
        Assert.Contains(SlotAlreadyApprovedMessage, result.Errors);
    }

    [Fact]
    public async Task Cancel_ReturnsFriendlyError_OnConcurrencyConflict()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var service = ServiceThatFailsToSave(ctx, new DbUpdateConcurrencyException("simulated"));
        var result = await service.CancelAppointmentAsync(id, "patient request");

        Assert.False(result.Success);
        Assert.Contains(ConcurrencyMessage, result.Errors);
    }

    [Fact]
    public async Task Reschedule_ReturnsFriendlyError_OnConcurrencyConflict()
    {
        using var ctx = new AppointmentTestContext();
        var id = ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var service = ServiceThatFailsToSave(ctx, new DbUpdateConcurrencyException("simulated"));
        var result = await service.RescheduleAppointmentAsync(id, AppointmentTestContext.TestDate.AddHours(14));

        Assert.False(result.Success);
        Assert.Contains(ConcurrencyMessage, result.Errors);
    }

    private static AppointmentService ServiceThatFailsToSave(AppointmentTestContext ctx, Exception onSave)
    {
        var throwingUow = new ThrowingUnitOfWork(ctx.UnitOfWork, onSave);
        return new AppointmentService(throwingUow, ctx.Notifications);
    }
}
