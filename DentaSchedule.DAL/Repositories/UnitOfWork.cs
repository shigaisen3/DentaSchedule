using DentaSchedule.DAL.Data;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.DAL.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentaScheduleDbContext _context;

    private IRepository<Clinic>? _clinics;
    private IRepository<Doctor>? _doctors;
    private IRepository<DoctorSchedule>? _doctorSchedules;
    private IRepository<ScheduleException>? _scheduleExceptions;
    private IRepository<Appointment>? _appointments;
    private IRepository<RefreshToken>? _refreshTokens;

    public UnitOfWork(DentaScheduleDbContext context)
    {
        _context = context;
    }

    public IRepository<Clinic> Clinics =>
        _clinics ??= new Repository<Clinic>(_context);

    public IRepository<Doctor> Doctors =>
        _doctors ??= new Repository<Doctor>(_context);

    public IRepository<DoctorSchedule> DoctorSchedules =>
        _doctorSchedules ??= new Repository<DoctorSchedule>(_context);

    public IRepository<ScheduleException> ScheduleExceptions =>
        _scheduleExceptions ??= new Repository<ScheduleException>(_context);

    public IRepository<Appointment> Appointments =>
        _appointments ??= new Repository<Appointment>(_context);

    public IRepository<RefreshToken> RefreshTokens =>
        _refreshTokens ??= new Repository<RefreshToken>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
