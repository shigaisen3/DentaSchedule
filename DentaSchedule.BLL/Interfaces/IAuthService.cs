using DentaSchedule.BLL.Common;

namespace DentaSchedule.BLL.Interfaces;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string? ClinicName { get; set; }
}

public interface IAuthService
{
    Task<ServiceResponse<AuthTokenResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResponse<AuthTokenResponse>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResponse> LogoutAsync(string refreshToken);
}
