using DentaSchedule.BLL.Common;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.BLL.Interfaces;

public interface IDoctorService
{
    Task<ServiceResponse<List<Doctor>>> GetDoctorsByClinicAsync(Guid clinicId);
    Task<ServiceResponse<Doctor>> GetDoctorByIdAsync(Guid id);
    Task<ServiceResponse<Doctor>> CreateDoctorAsync(Doctor doctor);
    Task<ServiceResponse<Doctor>> UpdateDoctorAsync(Doctor doctor);
    Task<ServiceResponse> DeleteDoctorAsync(Guid id);

    // Schedule management
    Task<ServiceResponse<List<DoctorSchedule>>> GetDoctorSchedulesAsync(Guid doctorId);
    Task<ServiceResponse<DoctorSchedule>> CreateScheduleAsync(DoctorSchedule schedule);
    Task<ServiceResponse<DoctorSchedule>> UpdateScheduleAsync(DoctorSchedule schedule);
    Task<ServiceResponse> DeleteScheduleAsync(Guid scheduleId);

    // Exception management
    Task<ServiceResponse<List<ScheduleException>>> GetDoctorExceptionsAsync(Guid doctorId);
    Task<ServiceResponse<ScheduleException>> CreateExceptionAsync(ScheduleException exception);
    Task<ServiceResponse> DeleteExceptionAsync(Guid exceptionId);
}
