using DentaSchedule.API.DTOs;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentaSchedule.API.Controllers;

[ApiController]
[Route("api/v1/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;

    public DoctorsController(IDoctorService doctorService, IAppointmentService appointmentService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Public: Get available time slots for a doctor on a specific date.
    /// </summary>
    [HttpGet("{id:guid}/available-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableSlots(Guid id, [FromQuery] DateTime date)
    {
        var result = await _appointmentService.GetAvailableSlotsAsync(id, date);
        if (!result.Success)
            return NotFound(new { result.Errors });

        var dtos = result.Data!.Select(s => new TimeSlotResponseDto
        {
            Time = s.Time.ToString(@"hh\:mm"),
            IsAvailable = s.IsAvailable
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Admin: Get doctor by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDoctor(Guid id)
    {
        var result = await _doctorService.GetDoctorByIdAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Admin: Create a new doctor.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        var doctor = new Doctor
        {
            FullName = dto.FullName,
            Specialization = dto.Specialization,
            Bio = dto.Bio,
            PhotoUrl = dto.PhotoUrl,
            ClinicId = dto.ClinicId
        };

        var result = await _doctorService.CreateDoctorAsync(doctor);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return CreatedAtAction(nameof(GetDoctor), new { id = result.Data!.Id }, MapToDto(result.Data));
    }

    /// <summary>
    /// Admin: Update doctor.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorDto dto)
    {
        var doctor = new Doctor
        {
            Id = id,
            FullName = dto.FullName,
            Specialization = dto.Specialization,
            Bio = dto.Bio,
            PhotoUrl = dto.PhotoUrl,
            IsActive = dto.IsActive,
            ClinicId = dto.ClinicId
        };

        var result = await _doctorService.UpdateDoctorAsync(doctor);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Admin: Soft-delete doctor.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var result = await _doctorService.DeleteDoctorAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return NoContent();
    }

    // ── Schedule endpoints ──

    [HttpGet("{id:guid}/schedule")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSchedules(Guid id)
    {
        var result = await _doctorService.GetDoctorSchedulesAsync(id);
        var dtos = result.Data!.Select(MapScheduleToDto).ToList();
        return Ok(dtos);
    }

    [HttpPost("{id:guid}/schedule")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSchedule(Guid id, [FromBody] CreateScheduleDto dto)
    {
        if (!TimeSpan.TryParse(dto.StartTime, out var startTime) ||
            !TimeSpan.TryParse(dto.EndTime, out var endTime))
            return BadRequest(new { Errors = new[] { "Invalid time format. Use HH:mm." } });

        var schedule = new DoctorSchedule
        {
            DoctorId = id,
            DayOfWeek = dto.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = dto.SlotDurationMinutes
        };

        var result = await _doctorService.CreateScheduleAsync(schedule);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Created($"api/v1/doctors/{id}/schedule", MapScheduleToDto(result.Data!));
    }

    [HttpPut("{doctorId:guid}/schedule/{scheduleId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSchedule(Guid doctorId, Guid scheduleId, [FromBody] UpdateScheduleDto dto)
    {
        if (!TimeSpan.TryParse(dto.StartTime, out var startTime) ||
            !TimeSpan.TryParse(dto.EndTime, out var endTime))
            return BadRequest(new { Errors = new[] { "Invalid time format. Use HH:mm." } });

        var schedule = new DoctorSchedule
        {
            Id = scheduleId,
            DoctorId = doctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = dto.SlotDurationMinutes
        };

        var result = await _doctorService.UpdateScheduleAsync(schedule);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(MapScheduleToDto(result.Data!));
    }

    [HttpDelete("{doctorId:guid}/schedule/{scheduleId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSchedule(Guid doctorId, Guid scheduleId)
    {
        var result = await _doctorService.DeleteScheduleAsync(scheduleId);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return NoContent();
    }

    // ── Exception endpoints ──

    [HttpGet("{id:guid}/exceptions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetExceptions(Guid id)
    {
        var result = await _doctorService.GetDoctorExceptionsAsync(id);
        var dtos = result.Data!.Select(MapExceptionToDto).ToList();
        return Ok(dtos);
    }

    [HttpPost("{id:guid}/exceptions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateException(Guid id, [FromBody] CreateExceptionDto dto)
    {
        var exception = new ScheduleException
        {
            DoctorId = id,
            ExceptionDate = dto.ExceptionDate,
            Reason = dto.Reason,
            IsFullDayOff = dto.IsFullDayOff
        };

        if (!dto.IsFullDayOff)
        {
            if (!string.IsNullOrEmpty(dto.CustomStart) && TimeSpan.TryParse(dto.CustomStart, out var cs))
                exception.CustomStart = cs;
            if (!string.IsNullOrEmpty(dto.CustomEnd) && TimeSpan.TryParse(dto.CustomEnd, out var ce))
                exception.CustomEnd = ce;
        }

        var result = await _doctorService.CreateExceptionAsync(exception);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Created($"api/v1/doctors/{id}/exceptions", MapExceptionToDto(result.Data!));
    }

    [HttpDelete("{doctorId:guid}/exceptions/{exceptionId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteException(Guid doctorId, Guid exceptionId)
    {
        var result = await _doctorService.DeleteExceptionAsync(exceptionId);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return NoContent();
    }

    // ── Mapping helpers ──

    private static DoctorDto MapToDto(Doctor d) => new()
    {
        Id = d.Id,
        FullName = d.FullName,
        Specialization = d.Specialization,
        Bio = d.Bio,
        PhotoUrl = d.PhotoUrl,
        IsActive = d.IsActive,
        ClinicId = d.ClinicId,
        ClinicName = d.Clinic?.Name
    };

    private static DoctorScheduleDto MapScheduleToDto(DoctorSchedule ds) => new()
    {
        Id = ds.Id,
        DayOfWeek = ds.DayOfWeek,
        StartTime = ds.StartTime.ToString(@"hh\:mm"),
        EndTime = ds.EndTime.ToString(@"hh\:mm"),
        SlotDurationMinutes = ds.SlotDurationMinutes
    };

    private static ScheduleExceptionDto MapExceptionToDto(ScheduleException se) => new()
    {
        Id = se.Id,
        ExceptionDate = se.ExceptionDate,
        Reason = se.Reason,
        IsFullDayOff = se.IsFullDayOff,
        CustomStart = se.CustomStart?.ToString(@"hh\:mm"),
        CustomEnd = se.CustomEnd?.ToString(@"hh\:mm")
    };
}
