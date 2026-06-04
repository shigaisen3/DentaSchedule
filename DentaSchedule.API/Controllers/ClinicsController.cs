using DentaSchedule.API.DTOs;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentaSchedule.API.Controllers;

[ApiController]
[Route("api/v1/clinics")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;
    private readonly IDoctorService _doctorService;

    public ClinicsController(IClinicService clinicService, IDoctorService doctorService)
    {
        _clinicService = clinicService;
        _doctorService = doctorService;
    }

    /// <summary>
    /// Public: List all active clinics.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetClinics()
    {
        var result = await _clinicService.GetActiveClinicsAsync();
        var dtos = result.Data!.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Public: Get doctors for a specific clinic.
    /// </summary>
    [HttpGet("{id:guid}/doctors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctorsByClinic(Guid id)
    {
        var result = await _doctorService.GetDoctorsByClinicAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        var dtos = result.Data!.Select(d => new DoctorDto
        {
            Id = d.Id,
            FullName = d.FullName,
            Specialization = d.Specialization,
            Bio = d.Bio,
            PhotoUrl = d.PhotoUrl,
            IsActive = d.IsActive,
            ClinicId = d.ClinicId
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Admin: Get clinic by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetClinic(Guid id)
    {
        var result = await _clinicService.GetClinicByIdAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Admin: Create a new clinic.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicDto dto)
    {
        var clinic = new Clinic
        {
            Name = dto.Name,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            LogoUrl = dto.LogoUrl
        };

        var result = await _clinicService.CreateClinicAsync(clinic);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return CreatedAtAction(nameof(GetClinic), new { id = result.Data!.Id }, MapToDto(result.Data));
    }

    /// <summary>
    /// Admin: Update an existing clinic.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateClinic(Guid id, [FromBody] UpdateClinicDto dto)
    {
        var clinic = new Clinic
        {
            Id = id,
            Name = dto.Name,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            LogoUrl = dto.LogoUrl,
            IsActive = dto.IsActive
        };

        var result = await _clinicService.UpdateClinicAsync(clinic);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(MapToDto(result.Data!));
    }

    /// <summary>
    /// Admin: Soft-delete a clinic.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteClinic(Guid id)
    {
        var result = await _clinicService.DeleteClinicAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return NoContent();
    }

    private static ClinicDto MapToDto(Clinic c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Address = c.Address,
        Phone = c.Phone,
        Email = c.Email,
        LogoUrl = c.LogoUrl,
        IsActive = c.IsActive
    };
}
