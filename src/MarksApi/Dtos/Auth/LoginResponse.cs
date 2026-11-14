namespace MarksApi.Dtos.Auth;

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Role);