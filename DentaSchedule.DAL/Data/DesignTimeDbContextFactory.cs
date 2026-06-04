using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentaSchedule.DAL.Data;

/// <summary>
/// Lets EF Core tooling (e.g. <c>dotnet ef migrations add</c>) construct the context
/// without booting the API host. Used only at design time; the running app configures
/// the context through dependency injection in <c>Program.cs</c>.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentaScheduleDbContext>
{
    public DentaScheduleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DentaScheduleDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=DentaScheduleDb;Trusted_Connection=true;" +
                "MultipleActiveResultSets=true;TrustServerCertificate=true")
            .Options;

        return new DentaScheduleDbContext(options);
    }
}
