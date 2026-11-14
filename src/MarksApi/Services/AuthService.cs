using System.Security.Claims;
using System.Text;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos.Auth;
using MarksApi.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MarksApi.Services;

public class AuthService(
    AppDbContext db,
    PasswordHasher<User> hasher,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username!.Trim();

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
        {
            logger.LogWarning("Login failed: unknown username {Username}", username);
            return null;
        }

        var check = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!);
        if (check == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: wrong password for {Username}", username);
            return null;
        }

        var jwt = jwtOptions.Value;
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("name", user.Username),
            new("role", user.Role)
        };
        if (user.StudentId is not null)
            claims.Add(new Claim("studentId", user.StudentId.Value.ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        logger.LogInformation("User {Username} logged in with role {Role}", user.Username, user.Role);
        return new LoginResponse(token, expires, user.Role);
    }
}