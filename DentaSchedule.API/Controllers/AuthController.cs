using DentaSchedule.API.DTOs;
using DentaSchedule.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DentaSchedule.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = dto.Email,
            Password = dto.Password
        });

        if (!result.Success)
            return Unauthorized(new { result.Errors });

        // Set refresh token in HttpOnly cookie
        SetRefreshTokenCookie(result.Data!.RefreshToken);

        return Ok(new AuthResponseDto
        {
            AccessToken = result.Data.AccessToken,
            RefreshToken = result.Data.RefreshToken,
            ExpiresAt = result.Data.ExpiresAt,
            DisplayName = result.Data.DisplayName,
            Email = result.Data.Email,
            Roles = result.Data.Roles,
            ClinicName = result.Data.ClinicName
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        // Try cookie first, fall back to body
        var refreshToken = Request.Cookies["refreshToken"] ?? dto.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { Errors = new[] { "Refresh token is required." } });

        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.Success)
            return Unauthorized(new { result.Errors });

        SetRefreshTokenCookie(result.Data!.RefreshToken);

        return Ok(new AuthResponseDto
        {
            AccessToken = result.Data.AccessToken,
            RefreshToken = result.Data.RefreshToken,
            ExpiresAt = result.Data.ExpiresAt,
            DisplayName = result.Data.DisplayName,
            Email = result.Data.Email,
            Roles = result.Data.Roles,
            ClinicName = result.Data.ClinicName
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        var refreshToken = Request.Cookies["refreshToken"] ?? dto.RefreshToken;

        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.LogoutAsync(refreshToken);

        Response.Cookies.Delete("refreshToken");

        return Ok(new { Message = "Logged out successfully." });
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
