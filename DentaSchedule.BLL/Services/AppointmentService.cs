using System.Security.Cryptography;
using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.BLL.Services;

public class AppointmentService : IAppointmentService
{
    private const string ConcurrencyMessage =
        "This appointment was just modified by someone else. Please reload and try again.";
    private const string SlotAlreadyApprovedMessage =
        "Another appointment has already been approved for this time slot.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentNotificationService _notifications;

    public AppointmentService(IUnitOfWork unitOfWork, IAppointmentNotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    /// <summary>
    /// Slot Generation Algorithm:
    /// 1. Load DoctorSchedule for the DayOfWeek matching the requested date.
    /// 2. Check ScheduleException — if full day off, return empty. If custom hours, use those.
    /// 3. Generate all theoretical slots: StartTime to EndTime, step = SlotDurationMinutes.
    /// 4. Load all non-Cancelled appointments for that doctor on that date.
    /// 5. Filter out slots that overlap with existing appointments.
    /// 6. Return available slots as List&lt;TimeSlotDto&gt;.
    /// </summary>
    public async Task<ServiceResponse<List<TimeSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId);
        if (doctor == null)
            return ServiceResponse<List<TimeSlotDto>>.FailureResult("Doctor not found.");

        // Step 1: Load schedule for the day of week
        var dayOfWeek = date.DayOfWeek;
        var schedules = await _unitOfWork.DoctorSchedules.Query()
            .Where(ds => ds.DoctorId == doctorId && ds.DayOfWeek == dayOfWeek)
            .ToListAsync();

        if (!schedules.Any())
            return ServiceResponse<List<TimeSlotDto>>.SuccessResult(new List<TimeSlotDto>());

        // Step 2: Check for schedule exceptions
        var dateOnly = date.Date;
        var exception = await _unitOfWork.ScheduleExceptions.Query()
            .FirstOrDefaultAsync(se => se.DoctorId == doctorId && se.ExceptionDate.Date == dateOnly);

        if (exception != null && exception.IsFullDayOff)
            return ServiceResponse<List<TimeSlotDto>>.SuccessResult(new List<TimeSlotDto>());

        // Step 3: Generate theoretical slots
        var allSlots = new List<TimeSlotDto>();

        foreach (var schedule in schedules)
        {
            var startTime = schedule.StartTime;
            var endTime = schedule.EndTime;
            var slotDuration = schedule.SlotDurationMinutes;

            // If custom hours from exception, override
            if (exception != null && !exception.IsFullDayOff)
            {
                if (exception.CustomStart.HasValue)
                    startTime = exception.CustomStart.Value;
                if (exception.CustomEnd.HasValue)
                    endTime = exception.CustomEnd.Value;
            }

            var currentSlot = startTime;
            while (currentSlot.Add(TimeSpan.FromMinutes(slotDuration)) <= endTime)
            {
                allSlots.Add(new TimeSlotDto
                {
                    Time = currentSlot,
                    IsAvailable = true
                });
                currentSlot = currentSlot.Add(TimeSpan.FromMinutes(slotDuration));
            }
        }

        // Step 4: Load approved appointments for this doctor on this date (only approved block slots)
        var existingAppointments = await _unitOfWork.Appointments.Query()
            .Where(a => a.DoctorId == doctorId
                        && a.AppointmentDateTime.Date == dateOnly
                        && a.Status == AppointmentStatus.Approved)
            .ToListAsync();

        // Step 5: Filter out occupied slots
        foreach (var slot in allSlots)
        {
            var slotStart = dateOnly.Add(slot.Time);
            foreach (var appt in existingAppointments)
            {
                var apptEnd = appt.AppointmentDateTime.AddMinutes(appt.DurationMinutes);
                // Check overlap
                if (slotStart < apptEnd && slotStart >= appt.AppointmentDateTime)
                {
                    slot.IsAvailable = false;
                    break;
                }
            }
        }

        // Don't return past slots for today (compare using local server time)
        var now = DateTime.Now;
        if (dateOnly == now.Date)
        {
            foreach (var slot in allSlots.Where(s => s.Time <= now.TimeOfDay))
            {
                slot.IsAvailable = false;
            }
        }

        return ServiceResponse<List<TimeSlotDto>>.SuccessResult(allSlots);
    }

    public async Task<ServiceResponse<Appointment>> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        if (request.AppointmentDateTime <= DateTime.Now)
            return ServiceResponse<Appointment>.FailureResult("Cannot create an appointment in the past.");

        // Verify doctor exists and belongs to the clinic
        var doctor = await _unitOfWork.Doctors.Query()
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.ClinicId == request.ClinicId && d.IsActive);

        if (doctor == null)
            return ServiceResponse<Appointment>.FailureResult("Doctor not found or not available at this clinic.");

        // Conflict detection
        var hasConflict = await HasConflictAsync(request.DoctorId, request.AppointmentDateTime, request.DurationMinutes);
        if (hasConflict)
            return ServiceResponse<Appointment>.FailureResult("The selected time slot is no longer available.");

        // Verify the slot is within the doctor's working hours
        var slotValid = await IsSlotWithinScheduleAsync(request.DoctorId, request.AppointmentDateTime, request.DurationMinutes);
        if (!slotValid)
            return ServiceResponse<Appointment>.FailureResult("The selected time is outside the doctor's working hours.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientName = request.PatientName,
            PatientEmail = request.PatientEmail,
            PatientPhone = request.PatientPhone,
            DoctorId = request.DoctorId,
            ClinicId = request.ClinicId,
            AppointmentDateTime = request.AppointmentDateTime,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
            Status = AppointmentStatus.Pending,
            ReferenceNumber = GenerateReferenceNumber(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        appointment.Doctor = doctor; // already loaded — lets the notifier skip a lookup
        await _notifications.NotifyCreatedAsync(appointment);

        return ServiceResponse<Appointment>.SuccessResult(appointment, "Appointment created successfully.");
    }

    public async Task<ServiceResponse<Appointment>> GetAppointmentByReferenceAsync(string referenceNumber)
    {
        var appointment = await _unitOfWork.Appointments.Query()
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a => a.ReferenceNumber == referenceNumber);

        if (appointment == null)
            return ServiceResponse<Appointment>.FailureResult("Appointment not found.");

        return ServiceResponse<Appointment>.SuccessResult(appointment);
    }

    public async Task<ServiceResponse<PagedResult<Appointment>>> GetAppointmentsAsync(AppointmentFilter filter)
    {
        var query = _unitOfWork.Appointments.Query()
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (filter.ClinicId.HasValue)
            query = query.Where(a => a.ClinicId == filter.ClinicId.Value);

        if (filter.Date.HasValue)
            query = query.Where(a => a.AppointmentDateTime.Date == filter.Date.Value.Date);

        if (filter.DateFrom.HasValue)
            query = query.Where(a => a.AppointmentDateTime >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(a => a.AppointmentDateTime < filter.DateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.AppointmentDateTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var result = new PagedResult<Appointment>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return ServiceResponse<PagedResult<Appointment>>.SuccessResult(result);
    }

    public async Task<ServiceResponse<Appointment>> ApproveAppointmentAsync(Guid id)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return ServiceResponse<Appointment>.FailureResult("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Pending)
            return ServiceResponse<Appointment>.FailureResult("Only pending appointments can be approved.");

        // Re-check at approval time: a different pending appointment may already have been
        // approved for an overlapping slot since this one was created.
        if (await HasConflictAsync(appointment.DoctorId, appointment.AppointmentDateTime, appointment.DurationMinutes, appointment.Id))
            return ServiceResponse<Appointment>.FailureResult(SlotAlreadyApprovedMessage);

        appointment.Status = AppointmentStatus.Approved;
        _unitOfWork.Appointments.Update(appointment);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResponse<Appointment>.FailureResult(ConcurrencyMessage);
        }
        catch (DbUpdateException)
        {
            // Unique-index violation: a concurrent request approved this slot first.
            return ServiceResponse<Appointment>.FailureResult(SlotAlreadyApprovedMessage);
        }

        await _notifications.NotifyApprovedAsync(appointment);

        return ServiceResponse<Appointment>.SuccessResult(appointment, "Appointment approved successfully.");
    }

    public async Task<ServiceResponse<Appointment>> CancelAppointmentAsync(Guid id, string reason)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return ServiceResponse<Appointment>.FailureResult("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResponse<Appointment>.FailureResult("Appointment is already cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = reason;
        _unitOfWork.Appointments.Update(appointment);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResponse<Appointment>.FailureResult(ConcurrencyMessage);
        }

        await _notifications.NotifyCancelledAsync(appointment);

        return ServiceResponse<Appointment>.SuccessResult(appointment, "Appointment cancelled successfully.");
    }

    public async Task<ServiceResponse<Appointment>> RescheduleAppointmentAsync(Guid id, DateTime newDateTime)
    {
        if (newDateTime <= DateTime.Now)
            return ServiceResponse<Appointment>.FailureResult("Cannot reschedule to a past date and time.");

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return ServiceResponse<Appointment>.FailureResult("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResponse<Appointment>.FailureResult("Cannot reschedule a cancelled appointment.");

        // Conflict detection for the new time
        var hasConflict = await HasConflictAsync(appointment.DoctorId, newDateTime, appointment.DurationMinutes, appointment.Id);
        if (hasConflict)
            return ServiceResponse<Appointment>.FailureResult("The selected time slot is not available.");

        var slotValid = await IsSlotWithinScheduleAsync(appointment.DoctorId, newDateTime, appointment.DurationMinutes);
        if (!slotValid)
            return ServiceResponse<Appointment>.FailureResult("The selected time is outside the doctor's working hours.");

        appointment.AppointmentDateTime = newDateTime;
        appointment.Status = AppointmentStatus.Pending; // Reset to pending after reschedule
        _unitOfWork.Appointments.Update(appointment);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResponse<Appointment>.FailureResult(ConcurrencyMessage);
        }

        await _notifications.NotifyRescheduledAsync(appointment);

        return ServiceResponse<Appointment>.SuccessResult(appointment, "Appointment rescheduled successfully.");
    }

    /// <summary>
    /// Conflict detection: checks if any Pending or Approved appointment overlaps the requested time window.
    /// </summary>
    private async Task<bool> HasConflictAsync(Guid doctorId, DateTime dateTime, int durationMinutes, Guid? excludeAppointmentId = null)
    {
        var requestedEnd = dateTime.AddMinutes(durationMinutes);

        var query = _unitOfWork.Appointments.Query()
            .Where(a => a.DoctorId == doctorId
                        && a.Status == AppointmentStatus.Approved
                        && a.AppointmentDateTime < requestedEnd
                        && a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > dateTime);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync();
    }

    private async Task<bool> IsSlotWithinScheduleAsync(Guid doctorId, DateTime dateTime, int durationMinutes)
    {
        var dayOfWeek = dateTime.DayOfWeek;
        var slotStart = dateTime.TimeOfDay;
        var slotEnd = slotStart.Add(TimeSpan.FromMinutes(durationMinutes));

        // Check for exceptions first
        var exception = await _unitOfWork.ScheduleExceptions.Query()
            .FirstOrDefaultAsync(se => se.DoctorId == doctorId && se.ExceptionDate.Date == dateTime.Date);

        if (exception != null)
        {
            if (exception.IsFullDayOff)
                return false;

            if (exception.CustomStart.HasValue && exception.CustomEnd.HasValue)
                return slotStart >= exception.CustomStart.Value && slotEnd <= exception.CustomEnd.Value;
        }

        // Check regular schedule
        var schedules = await _unitOfWork.DoctorSchedules.Query()
            .Where(ds => ds.DoctorId == doctorId && ds.DayOfWeek == dayOfWeek)
            .ToListAsync();

        return schedules.Any(s => slotStart >= s.StartTime && slotEnd <= s.EndTime);
    }

    public async Task<ServiceResponse<DashboardStats>> GetDashboardStatsAsync(Guid? clinicId)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        var weekEnd = weekStart.AddDays(7);

        var baseQuery = _unitOfWork.Appointments.Query();
        if (clinicId.HasValue)
            baseQuery = baseQuery.Where(a => a.ClinicId == clinicId.Value);

        var totalAppointments = await baseQuery.CountAsync();

        var todayAppointments = await baseQuery
            .Where(a => a.AppointmentDateTime.Date == today)
            .ToListAsync();

        var thisWeek = await baseQuery
            .CountAsync(a => a.AppointmentDateTime >= weekStart && a.AppointmentDateTime < weekEnd);

        var totalClinics = clinicId.HasValue ? 1 : await _unitOfWork.Clinics.Query().CountAsync();
        var totalDoctors = clinicId.HasValue
            ? await _unitOfWork.Doctors.Query().CountAsync(d => d.ClinicId == clinicId.Value)
            : await _unitOfWork.Doctors.Query().CountAsync();

        var recent = await baseQuery
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        var stats = new DashboardStats
        {
            TotalAppointments = totalAppointments,
            PendingToday = todayAppointments.Count(a => a.Status == AppointmentStatus.Pending),
            ApprovedToday = todayAppointments.Count(a => a.Status == AppointmentStatus.Approved),
            CancelledToday = todayAppointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            ThisWeek = thisWeek,
            TotalClinics = totalClinics,
            TotalDoctors = totalDoctors,
            RecentAppointments = recent.Select(a => new RecentAppointment
            {
                ReferenceNumber = a.ReferenceNumber,
                PatientName = a.PatientName,
                DoctorName = a.Doctor?.FullName ?? "—",
                ClinicName = a.Clinic?.Name ?? "—",
                AppointmentDateTime = a.AppointmentDateTime,
                Status = a.Status.ToString()
            }).ToList()
        };

        return ServiceResponse<DashboardStats>.SuccessResult(stats);
    }

    private static string GenerateReferenceNumber()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var number = BitConverter.ToUInt32(bytes) % 100000000;
        return $"DS-{number:D8}";
    }
}
