using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.BLL.Services;

public class ClinicService : IClinicService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClinicService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<List<Clinic>>> GetActiveClinicsAsync()
    {
        var clinics = await _unitOfWork.Clinics.Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return ServiceResponse<List<Clinic>>.SuccessResult(clinics);
    }

    public async Task<ServiceResponse<Clinic>> GetClinicByIdAsync(Guid id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic == null)
            return ServiceResponse<Clinic>.FailureResult("Clinic not found.");

        return ServiceResponse<Clinic>.SuccessResult(clinic);
    }

    public async Task<ServiceResponse<Clinic>> CreateClinicAsync(Clinic clinic)
    {
        clinic.Id = Guid.NewGuid();
        await _unitOfWork.Clinics.AddAsync(clinic);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<Clinic>.SuccessResult(clinic, "Clinic created successfully.");
    }

    public async Task<ServiceResponse<Clinic>> UpdateClinicAsync(Clinic clinic)
    {
        var existing = await _unitOfWork.Clinics.GetByIdAsync(clinic.Id);
        if (existing == null)
            return ServiceResponse<Clinic>.FailureResult("Clinic not found.");

        existing.Name = clinic.Name;
        existing.Address = clinic.Address;
        existing.Phone = clinic.Phone;
        existing.Email = clinic.Email;
        existing.LogoUrl = clinic.LogoUrl;
        existing.IsActive = clinic.IsActive;

        _unitOfWork.Clinics.Update(existing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<Clinic>.SuccessResult(existing, "Clinic updated successfully.");
    }

    public async Task<ServiceResponse> DeleteClinicAsync(Guid id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic == null)
            return ServiceResponse.FailureResult("Clinic not found.");

        // Soft delete
        clinic.IsDeleted = true;
        clinic.IsActive = false;
        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse.SuccessResult("Clinic deleted successfully.");
    }

    public async Task<ServiceResponse> ToggleActiveAsync(Guid id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic == null)
            return ServiceResponse.FailureResult("Clinic not found.");

        clinic.IsActive = !clinic.IsActive;
        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse.SuccessResult(
            clinic.IsActive ? "Clinic activated." : "Clinic deactivated.");
    }
}
