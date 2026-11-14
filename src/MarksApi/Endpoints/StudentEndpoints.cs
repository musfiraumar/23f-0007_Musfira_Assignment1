using System.Security.Claims;
using MarksApi.Dtos.Students;
using MarksApi.Infrastructure;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        group.MapGet("/", async (IStudentService service, CancellationToken ct,
                int page = 1, int pageSize = 10, string? search = null, string? sort = null) =>
                Results.Ok(await service.ListAsync(page, pageSize, search, sort, ct)))
            .RequireAuthorization("TeacherOnly");

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user,
                IStudentService service, CancellationToken ct) =>
                (await service.GetByIdAsync(id, user, ct)).ToHttpResult())
            .RequireAuthorization()
            .WithName("GetStudent");

        group.MapPost("/", async (StudentRequest request, IStudentService service, CancellationToken ct) =>
            {
                var errors = StudentRequestValidator.Validate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var result = await service.CreateAsync(request, ct);
                return result.ToCreatedResult(s => $"/api/students/{s.Id}");
            })
            .RequireAuthorization("TeacherOnly");

        group.MapPut("/{id:int}", async (int id, StudentRequest request,
                IStudentService service, CancellationToken ct) =>
            {
                var errors = StudentRequestValidator.Validate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                return (await service.UpdateAsync(id, request, ct)).ToHttpResult();
            })
            .RequireAuthorization("TeacherOnly");

        group.MapDelete("/{id:int}", async (int id, IStudentService service, CancellationToken ct) =>
                (await service.DeleteAsync(id, ct)).ToNoContentResult())
            .RequireAuthorization("TeacherOnly");

        return app;
    }
}