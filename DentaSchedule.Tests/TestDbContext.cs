using DentaSchedule.DAL.Data;
using DentaSchedule.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.Tests;

/// <summary>
/// Test-only context that relaxes the <c>Appointment.RowVersion</c> concurrency token.
/// SQL Server auto-generates the rowversion, but the EF Core in-memory provider does not —
/// it would otherwise reject every insert because the required property is null.
/// All other mapping is inherited unchanged from <see cref="DentaScheduleDbContext"/>.
/// </summary>
public class TestDbContext : DentaScheduleDbContext
{
    public TestDbContext(DbContextOptions<DentaScheduleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Appointment>()
            .Property(a => a.RowVersion)
            .IsConcurrencyToken(false)
            .ValueGeneratedNever()
            .IsRequired(false);
    }
}
