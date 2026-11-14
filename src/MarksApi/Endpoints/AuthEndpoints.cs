using MarksApi.Dtos.Auth;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Username))
                errors["username"] = ["Username is required."];
            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = ["Password is required."];
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var result = await auth.LoginAsync(request, ct);
            return result is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid credentials",
                    detail: "Username or password is incorrect.")
                : Results.Ok(result);
        })
        .AllowAnonymous();
    }
}