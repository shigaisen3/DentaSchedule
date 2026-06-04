using DentaSchedule.DAL.Entities;
using Xunit;

namespace DentaSchedule.Tests;

/// <summary>
/// Covers <c>AppointmentService.GetAvailableSlotsAsync</c> — the slot-generation algorithm:
/// schedule expansion, exception handling, and blocking by existing appointments.
/// </summary>
public class SlotGenerationTests
{
    [Fact]
    public async Task ReturnsFailure_WhenDoctorDoesNotExist()
    {
        using var ctx = new AppointmentTestContext();

        var result = await ctx.Service.GetAvailableSlotsAsync(Guid.NewGuid(), AppointmentTestContext.TestDate);

        Assert.False(result.Success);
        Assert.Contains("Doctor not found.", result.Errors);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenDoctorHasNoScheduleForThatDay()
    {
        // No schedule seeded at all.
        using var ctx = new AppointmentTestContext(seedDefaultSchedule: false);

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GeneratesCorrectNumberOfSlots_ForNineToFiveThirtyMinutes()
    {
        using var ctx = new AppointmentTestContext();

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        // 09:00–17:00 in 30-min steps = 16 slots, all free.
        Assert.True(result.Success);
        Assert.Equal(16, result.Data!.Count);
        Assert.All(result.Data!, slot => Assert.True(slot.IsAvailable));
        Assert.Equal(new TimeSpan(9, 0, 0), result.Data!.First().Time);
        Assert.Equal(new TimeSpan(16, 30, 0), result.Data!.Last().Time);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenExceptionIsFullDayOff()
    {
        using var ctx = new AppointmentTestContext();
        ctx.AddException(new ScheduleException
        {
            ExceptionDate = AppointmentTestContext.TestDate,
            IsFullDayOff = true,
            Reason = "Holiday"
        });

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task UsesCustomHours_WhenExceptionOverridesSchedule()
    {
        using var ctx = new AppointmentTestContext();
        // Override the 09:00–17:00 day with a short 10:00–12:00 window.
        ctx.AddException(new ScheduleException
        {
            ExceptionDate = AppointmentTestContext.TestDate,
            IsFullDayOff = false,
            CustomStart = new TimeSpan(10, 0, 0),
            CustomEnd = new TimeSpan(12, 0, 0)
        });

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        // 10:00–12:00 in 30-min steps = 4 slots.
        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.Count);
        Assert.Equal(new TimeSpan(10, 0, 0), result.Data!.First().Time);
        Assert.Equal(new TimeSpan(11, 30, 0), result.Data!.Last().Time);
    }

    [Fact]
    public async Task MarksSlotUnavailable_WhenApprovedAppointmentOccupiesIt()
    {
        using var ctx = new AppointmentTestContext();
        ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Approved);

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        var tenAm = result.Data!.Single(s => s.Time == new TimeSpan(10, 0, 0));
        Assert.False(tenAm.IsAvailable);

        // A neighbouring slot stays free.
        var nineAm = result.Data!.Single(s => s.Time == new TimeSpan(9, 0, 0));
        Assert.True(nineAm.IsAvailable);
    }

    [Fact]
    public async Task DoesNotBlockSlot_WhenAppointmentIsOnlyPending()
    {
        // Documents current behaviour: pending bookings do NOT reserve a slot;
        // only approved appointments block availability.
        using var ctx = new AppointmentTestContext();
        ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Pending);

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        var tenAm = result.Data!.Single(s => s.Time == new TimeSpan(10, 0, 0));
        Assert.True(tenAm.IsAvailable);
    }

    [Fact]
    public async Task DoesNotBlockSlot_WhenAppointmentIsCancelled()
    {
        using var ctx = new AppointmentTestContext();
        ctx.AddAppointment(new TimeSpan(10, 0, 0), AppointmentStatus.Cancelled);

        var result = await ctx.Service.GetAvailableSlotsAsync(
            AppointmentTestContext.DoctorId, AppointmentTestContext.TestDate);

        var tenAm = result.Data!.Single(s => s.Time == new TimeSpan(10, 0, 0));
        Assert.True(tenAm.IsAvailable);
    }
}
