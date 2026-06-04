using Microsoft.AspNetCore.Identity;

namespace DentaSchedule.DAL.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    // Optional FK to Clinic (for Assistants assigned to a clinic)
    public Guid? ClinicId { get; set; }

    // Navigation
    public Clinic? Clinic { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
