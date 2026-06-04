using DentaSchedule.API.DTOs;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentaSchedule.API.Controllers;

[ApiController]
[Route("api/v1/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>
    /// Assistant/Admin/Nurse: Get dashboard statistics.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Assistant,Admin")]
    public async Task<IActionResult> GetStats()
    {
        Guid? clinicId = null;
        if (User.IsInRole("Assistant") && !User.IsInRole("Admin"))
        {
            var clinicIdClaim = User.FindFirst("ClinicId")?.Value;
            if (Guid.TryParse(clinicIdClaim, out var cid))
                clinicId = cid;
        }

        var result = await _appointmentService.GetDashboardStatsAsync(clinicId);
        return Ok(result.Data);
    }

    /// <summary>
    /// Public: Create a new pending appointment.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var request = new CreateAppointmentRequest
        {
            PatientName = dto.PatientName,
            PatientEmail = dto.PatientEmail,
            PatientPhone = dto.PatientPhone,
            DoctorId = dto.DoctorId,
            ClinicId = dto.ClinicId,
            AppointmentDateTime = dto.AppointmentDateTime,
            DurationMinutes = dto.DurationMinutes,
            Notes = dto.Notes
        };

        var result = await _appointmentService.CreateAppointmentAsync(request);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Created($"api/v1/appointments/{result.Data!.ReferenceNumber}", MapToDto(result.Data));
    }

    /// <summary>
    /// Public: Get appointment by reference number (patient lookup).
    /// </summary>
    [HttpGet("{reference}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByReference(string reference)
    {
        var result = await _appointmentService.GetAppointmentByReferenceAsync(reference);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Assistant/Admin: Get paginated appointment list with filters.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Assistant,Admin")]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] string? status,
        [FromQuery] Guid? clinicId,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new AppointmentFilter
        {
            Status = Enum.TryParse<AppointmentStatus>(status, true, out var s) ? s : null,
            ClinicId = clinicId,
            Date = date,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page,
            PageSize = pageSize
        };

        // If user is an Assistant or Nurse (not Admin), scope to their clinic
        if (User.IsInRole("Assistant") && !User.IsInRole("Admin"))
        {
            var clinicIdClaim = User.FindFirst("ClinicId")?.Value;
            if (Guid.TryParse(clinicIdClaim, out var assistantClinicId))
                filter.ClinicId = assistantClinicId;
        }

        var result = await _appointmentService.GetAppointmentsAsync(filter);
        var pagedResult = result.Data!;

        return Ok(new PagedResultDto<AppointmentDto>
        {
            Items = pagedResult.Items.Select(MapToDto).ToList(),
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage
        });
    }

    /// <summary>
    /// Assistant/Admin: Approve a pending appointment.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Assistant,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _appointmentService.ApproveAppointmentAsync(id);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Assistant/Admin: Cancel an appointment with a reason.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Assistant,Admin")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelDto dto)
    {
        var result = await _appointmentService.CancelAppointmentAsync(id, dto.Reason);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Assistant/Admin: Reschedule an appointment.
    /// </summary>
    [HttpPut("{id:guid}/reschedule")]
    [Authorize(Roles = "Assistant,Admin")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleDto dto)
    {
        var result = await _appointmentService.RescheduleAppointmentAsync(id, dto.NewDateTime);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PatientName = a.PatientName,
        PatientEmail = a.PatientEmail,
        PatientPhone = a.PatientPhone,
        AppointmentDateTime = a.AppointmentDateTime,
        DurationMinutes = a.DurationMinutes,
        Status = a.Status.ToString(),
        Notes = a.Notes,
        CancellationReason = a.CancellationReason,
        ReferenceNumber = a.ReferenceNumber,
        CreatedAt = a.CreatedAt,
        DoctorId = a.DoctorId,
        DoctorName = a.Doctor?.FullName,
        ClinicId = a.ClinicId,
        ClinicName = a.Clinic?.Name
    };
}
