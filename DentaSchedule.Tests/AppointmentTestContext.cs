using DentaSchedule.BLL.Services;
using DentaSchedule.DAL.Data;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.Tests;

/// <summary>
/// Builds an isolated in-memory database (real DbContext + UnitOfWork + repositories)
/// and exposes an <see cref="AppointmentService"/> wired to it. Each instance gets a
/// uniquely-named store, so tests are fully isolated and can run in parallel.
///
/// A clinic, an active doctor, and a Mon–Sun 09:00–17:00 / 30-min schedule are seeded
/// by default so slot-generation tests have something to work with.
/// </summary>
public sealed class AppointmentTestContext : IDisposable
{
    public static readonly Guid ClinicId = new("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid DoctorId = new("bbbbbbbb-0000-0000-0000-000000000001");

    /// <summary>A date one week out — always in the future, so the "past slot" filter never interferes.</summary>
    public static readonly DateTime TestDate = DateTime.Today.AddDays(7);

    public DentaScheduleDbContext Context { get; }
    public IUnitOfWork UnitOfWork { get; }
    public AppointmentService Service { get; }

    /// <summary>Captures which lifecycle notifications the service fired.</summary>
    public RecordingNotificationService Notifications { get; } = new();

    public AppointmentTestContext(bool seedDefaultSchedule = true)
    {
        var options = new DbContextOptionsBuilder<DentaScheduleDbContext>()
            .UseInMemoryDatabase($"DentaSchedule_Test_{Guid.NewGuid()}")
            .Options;

        Context = new TestDbContext(options);

        Context.Clinics.Add(new Clinic
        {
            Id = ClinicId,
            Name = "Test Clinic",
            Address = "Str. Test 1",
            Phone = "0700000000",
            Email = "test@clinic.ro",
            IsActive = true
        });

        Context.Doctors.Add(new Doctor
        {
            Id = DoctorId,
            FullName = "Dr. Test",
            Specialization = "General",
            ClinicId = ClinicId,
            IsActive = true
        });

        if (seedDefaultSchedule)
        {
            // Seed every day of the week so any TestDate has working hours.
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                Context.DoctorSchedules.Add(new DoctorSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = DoctorId,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    SlotDurationMinutes = 30
                });
            }
        }

        Context.SaveChanges();

        UnitOfWork = new UnitOfWork(Context);
        Service = new AppointmentService(UnitOfWork, Notifications);
    }

    /// <summary>Adds an appointment at the given time on <see cref="TestDate"/> and returns its id.</summary>
    public Guid AddAppointment(TimeSpan time, AppointmentStatus status, int durationMinutes = 30)
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientName = "Patient",
            PatientEmail = "patient@test.ro",
            PatientPhone = "0700000001",
            DoctorId = DoctorId,
            ClinicId = ClinicId,
            AppointmentDateTime = TestDate.Add(time),
            DurationMinutes = durationMinutes,
            Status = status,
            ReferenceNumber = $"DS-{Random.Shared.Next(10000000, 99999999)}",
            CreatedAt = DateTime.UtcNow
        };

        Context.Appointments.Add(appointment);
        Context.SaveChanges();
        return appointment.Id;
    }

    public void AddException(ScheduleException exception)
    {
        exception.Id = exception.Id == Guid.Empty ? Guid.NewGuid() : exception.Id;
        exception.DoctorId = DoctorId;
        Context.ScheduleExceptions.Add(exception);
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
