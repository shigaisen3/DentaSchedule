using DentaSchedule.API.DTOs;
using DentaSchedule.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentaSchedule.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _userService.GetAllUsersAsync();

        var dtos = result.Data!.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Roles = u.Roles,
            ClinicId = u.ClinicId,
            IsLockedOut = u.IsLockedOut
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        var u = result.Data!;
        return Ok(new UserResponseDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Roles = u.Roles,
            ClinicId = u.ClinicId,
            IsLockedOut = u.IsLockedOut
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(new CreateUserRequest
        {
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            Password = dto.Password,
            Role = dto.Role,
            ClinicId = dto.ClinicId
        });

        if (!result.Success)
            return BadRequest(new { result.Errors });

        var u = result.Data!;
        return CreatedAtAction(nameof(GetUser), new { id = u.Id }, new UserResponseDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Roles = u.Roles,
            ClinicId = u.ClinicId
        });
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AddRole(string id, [FromBody] AddRoleDto dto)
    {
        var result = await _userService.AddRoleAsync(id, dto.Role);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(new { result.Message });
    }

    [HttpDelete("{id}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var result = await _userService.RemoveRoleAsync(id, role);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(new { result.Message });
    }

    [HttpPut("{id}/lockout")]
    public async Task<IActionResult> LockoutUser(string id)
    {
        var result = await _userService.LockoutUserAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(new { result.Message });
    }

    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var result = await _userService.UnlockUserAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return Ok(new { result.Message });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordDto dto)
    {
        var result = await _userService.ResetPasswordAsync(id, dto.NewPassword);
        if (!result.Success)
            return BadRequest(new { result.Errors });

        return Ok(new { result.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result.Success)
            return NotFound(new { result.Errors });

        return NoContent();
    }
}
