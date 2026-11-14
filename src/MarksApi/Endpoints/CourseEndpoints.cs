using MarksApi.Dtos.Courses;
using MarksApi.Infrastructure;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        // Any logged-in user (Teacher or Student)
        group.MapGet("/", async (ICourseService service, CancellationToken ct,
                int page = 1, int pageSize = 10, string? search = null, string? sort = null) =>
                Results.Ok(await service.ListAsync(page, pageSize, search, sort, ct)))
            .RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, ICourseService service, CancellationToken ct) =>
                (await service.GetByIdAsync(id, ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/", async (CourseRequest request, ICourseService service, CancellationToken ct) =>
            {
                var errors = CourseRequestValidator.Validate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var result = await service.CreateAsync(request, ct);
                return result.ToCreatedResult(c => $"/api/courses/{c.Id}");
            })
            .RequireAuthorization("TeacherOnly");

        group.MapPut("/{id:int}", async (int id, CourseRequest request,
                ICourseService service, CancellationToken ct) =>
            {
                var errors = CourseRequestValidator.Validate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                return (await service.UpdateAsync(id, request, ct)).ToHttpResult();
            })
            .RequireAuthorization("TeacherOnly");

        group.MapDelete("/{id:int}", async (int id, ICourseService service, CancellationToken ct) =>
                (await service.DeleteAsync(id, ct)).ToNoContentResult())
            .RequireAuthorization("TeacherOnly");

        return app;
    }
}