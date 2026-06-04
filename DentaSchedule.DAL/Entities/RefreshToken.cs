namespace DentaSchedule.DAL.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = string.Empty;

    // Navigation
    public AppUser User { get; set; } = null!;
}
