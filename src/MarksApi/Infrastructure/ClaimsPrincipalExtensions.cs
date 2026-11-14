using System.Security.Claims;

namespace MarksApi.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static bool IsTeacher(this ClaimsPrincipal user) => user.HasClaim("role", "Teacher");

    public static int? GetStudentId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("studentId"), out var id) ? id : null;
}