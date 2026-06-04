using DentaSchedule.DAL.Entities;

namespace DentaSchedule.DAL.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<Clinic> Clinics { get; }
    IRepository<Doctor> Doctors { get; }
    IRepository<DoctorSchedule> DoctorSchedules { get; }
    IRepository<ScheduleException> ScheduleExceptions { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync();
}
