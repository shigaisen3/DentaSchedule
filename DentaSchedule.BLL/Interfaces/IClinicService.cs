using DentaSchedule.BLL.Common;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.BLL.Interfaces;

public interface IClinicService
{
    Task<ServiceResponse<List<Clinic>>> GetActiveClinicsAsync();
    Task<ServiceResponse<Clinic>> GetClinicByIdAsync(Guid id);
    Task<ServiceResponse<Clinic>> CreateClinicAsync(Clinic clinic);
    Task<ServiceResponse<Clinic>> UpdateClinicAsync(Clinic clinic);
    Task<ServiceResponse> DeleteClinicAsync(Guid id);
    Task<ServiceResponse> ToggleActiveAsync(Guid id);
}
