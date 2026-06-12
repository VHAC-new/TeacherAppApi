using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeacherApp.Api.Data;
using TeacherApp.Api.Domain;
using TeacherApp.Contracts.Auth;

namespace TeacherApp.Api.Application.Auth;

public sealed class AuthService(AppDbContext db, IConfiguration configuration) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(2);
        var token = CreateJwt(user, expiresAtUtc);

        return new LoginResponse(
            AccessToken: token,
            ExpiresAtUtc: expiresAtUtc,
            UserId: user.Id,
            Email: user.Email,
            Roles: new[] { user.Role }
        );
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ArgumentException("Current password and new password are required.");

        if (request.NewPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
    }

    private string CreateJwt(User user, DateTimeOffset expiresAtUtc)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var secretEnvVarName = configuration["Jwt:SecretEnvVarName"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secretEnvVarName))
        {
            throw new InvalidOperationException("Missing JWT configuration.");
        }

        var secret = Environment.GetEnvironmentVariable(secretEnvVarName);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException($"Missing JWT secret environment variable: {secretEnvVarName}");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.Name ?? ""),
            new(ClaimTypes.Role, user.Role),
        };

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

