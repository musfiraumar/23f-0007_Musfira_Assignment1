using MarksApi.Dtos.Auth;

namespace MarksApi.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
}