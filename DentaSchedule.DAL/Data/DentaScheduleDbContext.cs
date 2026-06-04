using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.DAL.Data;

public class DentaScheduleDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public DentaScheduleDbContext(DbContextOptions<DentaScheduleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<ScheduleException> ScheduleExceptions => Set<ScheduleException>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureClinic(builder);
        ConfigureDoctor(builder);
        ConfigureDoctorSchedule(builder);
        ConfigureScheduleException(builder);
        ConfigureAppointment(builder);
        ConfigureAppUser(builder);
        ConfigureRefreshToken(builder);

        // Database-level guarantee against double-booking: at most one *approved*
        // appointment may exist per doctor + start time. This is the final safety net
        // for the race where two pending appointments for the same slot are approved
        // concurrently. Relational-only (the in-memory provider used in tests does not
        // support filtered indexes).
        if (Database.IsRelational())
        {
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDateTime })
                .HasFilter("[Status] = 'Approved'")
                .IsUnique()
                .HasDatabaseName("IX_Appointments_Doctor_Time_Approved");
        }
    }

    private static void ConfigureClinic(ModelBuilder builder)
    {
        builder.Entity<Clinic>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Address).HasMaxLength(500).IsRequired();
            entity.Property(c => c.Phone).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(200).IsRequired();
            entity.Property(c => c.LogoUrl).HasMaxLength(500);

            // Soft delete global filter
            entity.HasQueryFilter(c => !c.IsDeleted);
        });
    }

    private static void ConfigureDoctor(ModelBuilder builder)
    {
        builder.Entity<Doctor>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FullName).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Specialization).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Bio).HasMaxLength(2000);
            entity.Property(d => d.PhotoUrl).HasMaxLength(500);

            entity.HasIndex(d => d.ClinicId);

            entity.HasOne(d => d.Clinic)
                .WithMany(c => c.Doctors)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete global filter
            entity.HasQueryFilter(d => !d.IsDeleted);
        });
    }

    private static void ConfigureDoctorSchedule(ModelBuilder builder)
    {
        builder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(ds => ds.Id);

            entity.HasOne(ds => ds.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ds => new { ds.DoctorId, ds.DayOfWeek });
        });
    }

    private static void ConfigureScheduleException(ModelBuilder builder)
    {
        builder.Entity<ScheduleException>(entity =>
        {
            entity.HasKey(se => se.Id);
            entity.Property(se => se.Reason).HasMaxLength(500);

            entity.HasOne(se => se.Doctor)
                .WithMany(d => d.Exceptions)
                .HasForeignKey(se => se.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(se => new { se.DoctorId, se.ExceptionDate });
        });
    }

    private static void ConfigureAppointment(ModelBuilder builder)
    {
        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.PatientEmail).HasMaxLength(200).IsRequired();
            entity.Property(a => a.PatientPhone).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Notes).HasMaxLength(1000);
            entity.Property(a => a.CancellationReason).HasMaxLength(500);
            entity.Property(a => a.ReferenceNumber).HasMaxLength(20).IsRequired();
            entity.Property(a => a.CreatedByUserId).HasMaxLength(450);

            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Optimistic concurrency
            entity.Property(a => a.RowVersion)
                .IsRowVersion();

            // Indexes
            entity.HasIndex(a => new { a.DoctorId, a.AppointmentDateTime });
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.ReferenceNumber).IsUnique();

            entity.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Clinic)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAppUser(ModelBuilder builder)
    {
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();

            entity.HasOne(u => u.Clinic)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder builder)
    {
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).HasMaxLength(500).IsRequired();

            entity.HasIndex(rt => rt.Token);

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Appointment>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
