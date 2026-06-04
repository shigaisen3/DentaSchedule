using DentaSchedule.BLL.Common;
using DentaSchedule.DAL.Entities;

namespace DentaSchedule.BLL.Interfaces;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? ClinicId { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Guid? ClinicId { get; set; }
    public bool IsLockedOut { get; set; }
}

public interface IUserService
{
    Task<ServiceResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ServiceResponse<UserDto>> GetUserByIdAsync(string id);
    Task<ServiceResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ServiceResponse> AddRoleAsync(string userId, string role);
    Task<ServiceResponse> RemoveRoleAsync(string userId, string role);
    Task<ServiceResponse> LockoutUserAsync(string userId);
    Task<ServiceResponse> UnlockUserAsync(string userId);
    Task<ServiceResponse> ResetPasswordAsync(string userId, string newPassword);
    Task<ServiceResponse> DeleteUserAsync(string userId);
}
