using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DentaSchedule.BLL.Common;
using DentaSchedule.BLL.Interfaces;
using DentaSchedule.DAL.Entities;
using DentaSchedule.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DentaSchedule.BLL.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<ServiceResponse<AuthTokenResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ServiceResponse<AuthTokenResponse>.FailureResult("Invalid email or password.");

        if (await _userManager.IsLockedOutAsync(user))
            return ServiceResponse<AuthTokenResponse>.FailureResult("Account is locked out.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
            return ServiceResponse<AuthTokenResponse>.FailureResult("Invalid email or password.");

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        string? clinicName = null;
        if (user.ClinicId.HasValue)
        {
            var clinic = await _unitOfWork.Clinics.Query()
                .FirstOrDefaultAsync(c => c.Id == user.ClinicId.Value);
            clinicName = clinic?.Name;
        }

        return ServiceResponse<AuthTokenResponse>.SuccessResult(new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            DisplayName = user.DisplayName,
            Email = user.Email!,
            Roles = roles,
            ClinicName = clinicName
        });
    }

    public async Task<ServiceResponse<AuthTokenResponse>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.RefreshTokens.Query()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (storedToken == null)
            return ServiceResponse<AuthTokenResponse>.FailureResult("Invalid refresh token.");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            storedToken.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResponse<AuthTokenResponse>.FailureResult("Refresh token has expired.");
        }

        // Revoke old token (single-use)
        storedToken.IsRevoked = true;

        var user = storedToken.User;
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var newAccessToken = GenerateAccessToken(user, roles);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        string? clinicName = null;
        if (user.ClinicId.HasValue)
        {
            var clinic = await _unitOfWork.Clinics.Query()
                .FirstOrDefaultAsync(c => c.Id == user.ClinicId.Value);
            clinicName = clinic?.Name;
        }

        await _unitOfWork.SaveChangesAsync();

        return ServiceResponse<AuthTokenResponse>.SuccessResult(new AuthTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            DisplayName = user.DisplayName,
            Email = user.Email!,
            Roles = roles,
            ClinicName = clinicName
        });
    }

    public async Task<ServiceResponse> LogoutAsync(string refreshToken)
    {
        var storedToken = await _unitOfWork.RefreshTokens.Query()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync();
        }

        return ServiceResponse.SuccessResult("Logged out successfully.");
    }

    private string GenerateAccessToken(AppUser user, List<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.ClinicId.HasValue)
            claims.Add(new Claim("ClinicId", user.ClinicId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var token = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(token);
        return token;
    }

    private int GetAccessTokenLifetimeMinutes()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenLifetimeMinutes"], out var minutes)
            ? minutes
            : 15;
    }
}
