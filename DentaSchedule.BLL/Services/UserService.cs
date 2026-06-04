using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DentaSchedule.BLL.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;

    public UserService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ServiceResponse<List<UserDto>>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Roles = roles.ToList(),
                ClinicId = user.ClinicId,
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }

        return ServiceResponse<List<UserDto>>.SuccessResult(userDtos);
    }

    public async Task<ServiceResponse<UserDto>> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return ServiceResponse<UserDto>.FailureResult("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        return ServiceResponse<UserDto>.SuccessResult(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Roles = roles.ToList(),
            ClinicId = user.ClinicId,
            IsLockedOut = await _userManager.IsLockedOutAsync(user)
        });
    }

    public async Task<ServiceResponse<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return ServiceResponse<UserDto>.FailureResult("A user with this email already exists.");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            ClinicId = request.ClinicId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return ServiceResponse<UserDto>.FailureResult(
                result.Errors.Select(e => e.Description).ToList());

        if (!string.IsNullOrEmpty(request.Role))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
                return ServiceResponse<UserDto>.FailureResult(
                    roleResult.Errors.Select(e => e.Description).ToList());
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ServiceResponse<UserDto>.SuccessResult(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles.ToList(),
            ClinicId = user.ClinicId
        }, "User created successfully.");
    }

    public async Task<ServiceResponse> AddRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return ServiceResponse.FailureResult(result.Errors.Select(e => e.Description).ToList());

        return ServiceResponse.SuccessResult("Role added successfully.");
    }

    public async Task<ServiceResponse> RemoveRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return ServiceResponse.FailureResult(result.Errors.Select(e => e.Description).ToList());

        return ServiceResponse.SuccessResult("Role removed successfully.");
    }

    public async Task<ServiceResponse> LockoutUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return ServiceResponse.SuccessResult("User locked out successfully.");
    }

    public async Task<ServiceResponse> UnlockUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);

        return ServiceResponse.SuccessResult("User unlocked successfully.");
    }

    public async Task<ServiceResponse> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            return ServiceResponse.FailureResult(result.Errors.Select(e => e.Description).ToList());

        return ServiceResponse.SuccessResult("Password reset successfully.");
    }

    public async Task<ServiceResponse> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResponse.FailureResult("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return ServiceResponse.FailureResult(result.Errors.Select(e => e.Description).ToList());

        return ServiceResponse.SuccessResult("User deleted successfully.");
    }
}
