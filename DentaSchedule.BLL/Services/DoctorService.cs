using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.BLL.Services;

public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;

    public DoctorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<List<Doctor>>> GetDoctorsByClinicAsync(Guid clinicId)
    {
        var doctors = await _unitOfWork.Doctors.Query()
            .Where(d => d.ClinicId == clinicId && d.IsActive)
            .OrderBy(d => d.FullName)
            .ToListAsync();

        return ServiceResponse<List<Doctor>>.SuccessResult(doctors);
    }

    public async Task<ServiceResponse<Doctor>> GetDoctorByIdAsync(Guid id)
    {
        var doctor = await _unitOfWork.Doctors.Query()
            .Include(d => d.Clinic)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return ServiceResponse<Doctor>.FailureResult("Doctor not found.");

        return ServiceResponse<Doctor>.SuccessResult(doctor);
    }

    public async Task<ServiceResponse<Doctor>> CreateDoctorAsync(Doctor doctor)
    {
        // Verify clinic exists
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(doctor.ClinicId);
        if (clinic == null)
            return ServiceResponse<Doctor>.FailureResult("Clinic not found.");

        doctor.Id = Guid.NewGuid();
        await _unitOfWork.Doctors.AddAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<Doctor>.SuccessResult(doctor, "Doctor created successfully.");
    }

    public async Task<ServiceResponse<Doctor>> UpdateDoctorAsync(Doctor doctor)
    {
        var existing = await _unitOfWork.Doctors.GetByIdAsync(doctor.Id);
        if (existing == null)
            return ServiceResponse<Doctor>.FailureResult("Doctor not found.");

        existing.FullName = doctor.FullName;
        existing.Specialization = doctor.Specialization;
        existing.Bio = doctor.Bio;
        existing.PhotoUrl = doctor.PhotoUrl;
        existing.IsActive = doctor.IsActive;
        existing.ClinicId = doctor.ClinicId;

        _unitOfWork.Doctors.Update(existing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<Doctor>.SuccessResult(existing, "Doctor updated successfully.");
    }

    public async Task<ServiceResponse> DeleteDoctorAsync(Guid id)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
        if (doctor == null)
            return ServiceResponse.FailureResult("Doctor not found.");

        // Soft delete
        doctor.IsDeleted = true;
        doctor.IsActive = false;
        _unitOfWork.Doctors.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse.SuccessResult("Doctor deleted successfully.");
    }

    // Schedule management
    public async Task<ServiceResponse<List<DoctorSchedule>>> GetDoctorSchedulesAsync(Guid doctorId)
    {
        var schedules = await _unitOfWork.DoctorSchedules.Query()
            .Where(ds => ds.DoctorId == doctorId)
            .OrderBy(ds => ds.DayOfWeek)
            .ThenBy(ds => ds.StartTime)
            .ToListAsync();

        return ServiceResponse<List<DoctorSchedule>>.SuccessResult(schedules);
    }

    public async Task<ServiceResponse<DoctorSchedule>> CreateScheduleAsync(DoctorSchedule schedule)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(schedule.DoctorId);
        if (doctor == null)
            return ServiceResponse<DoctorSchedule>.FailureResult("Doctor not found.");

        if (schedule.StartTime >= schedule.EndTime)
            return ServiceResponse<DoctorSchedule>.FailureResult("Start time must be before end time.");

        // Check for overlapping schedules on the same day
        var existingSchedules = await _unitOfWork.DoctorSchedules.Query()
            .Where(ds => ds.DoctorId == schedule.DoctorId && ds.DayOfWeek == schedule.DayOfWeek)
            .ToListAsync();

        var hasOverlap = existingSchedules.Any(ds =>
            schedule.StartTime < ds.EndTime && schedule.EndTime > ds.StartTime);

        if (hasOverlap)
            return ServiceResponse<DoctorSchedule>.FailureResult(
                "Schedule overlaps with an existing schedule for this day.");

        schedule.Id = Guid.NewGuid();
        await _unitOfWork.DoctorSchedules.AddAsync(schedule);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<DoctorSchedule>.SuccessResult(schedule, "Schedule created successfully.");
    }

    public async Task<ServiceResponse<DoctorSchedule>> UpdateScheduleAsync(DoctorSchedule schedule)
    {
        var existing = await _unitOfWork.DoctorSchedules.GetByIdAsync(schedule.Id);
        if (existing == null)
            return ServiceResponse<DoctorSchedule>.FailureResult("Schedule not found.");

        if (schedule.StartTime >= schedule.EndTime)
            return ServiceResponse<DoctorSchedule>.FailureResult("Start time must be before end time.");

        existing.DayOfWeek = schedule.DayOfWeek;
        existing.StartTime = schedule.StartTime;
        existing.EndTime = schedule.EndTime;
        existing.SlotDurationMinutes = schedule.SlotDurationMinutes;

        _unitOfWork.DoctorSchedules.Update(existing);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<DoctorSchedule>.SuccessResult(existing, "Schedule updated successfully.");
    }

    public async Task<ServiceResponse> DeleteScheduleAsync(Guid scheduleId)
    {
        var schedule = await _unitOfWork.DoctorSchedules.GetByIdAsync(scheduleId);
        if (schedule == null)
            return ServiceResponse.FailureResult("Schedule not found.");

        _unitOfWork.DoctorSchedules.Remove(schedule);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse.SuccessResult("Schedule deleted successfully.");
    }

    // Exception management
    public async Task<ServiceResponse<List<ScheduleException>>> GetDoctorExceptionsAsync(Guid doctorId)
    {
        var exceptions = await _unitOfWork.ScheduleExceptions.Query()
            .Where(se => se.DoctorId == doctorId)
            .OrderBy(se => se.ExceptionDate)
            .ToListAsync();

        return ServiceResponse<List<ScheduleException>>.SuccessResult(exceptions);
    }

    public async Task<ServiceResponse<ScheduleException>> CreateExceptionAsync(ScheduleException exception)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(exception.DoctorId);
        if (doctor == null)
            return ServiceResponse<ScheduleException>.FailureResult("Doctor not found.");

        exception.Id = Guid.NewGuid();
        await _unitOfWork.ScheduleExceptions.AddAsync(exception);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<ScheduleException>.SuccessResult(exception, "Exception created successfully.");
    }

    public async Task<ServiceResponse> DeleteExceptionAsync(Guid exceptionId)
    {
        var exception = await _unitOfWork.ScheduleExceptions.GetByIdAsync(exceptionId);
        if (exception == null)
            return ServiceResponse.FailureResult("Schedule exception not found.");

        _unitOfWork.ScheduleExceptions.Remove(exception);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse.SuccessResult("Exception deleted successfully.");
    }
}
