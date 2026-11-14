using MarksApi.Dtos.Enrollments;
using MarksApi.Infrastructure;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var byCourse = app.MapGroup("/api/courses/{courseId:int}/enrollments").WithTags("Enrollments");

        byCourse.MapGet("/", async (int courseId, IEnrollmentService service, CancellationToken ct,
                int page = 1, int pageSize = 10, string? search = null, string? sort = null) =>
                (await service.ListByCourseAsync(courseId, page, pageSize, search, sort, ct)).ToHttpResult())
            .RequireAuthorization("TeacherOnly");

        byCourse.MapPost("/", async (int courseId, EnrollmentCreateRequest request,
                IEnrollmentService service, CancellationToken ct) =>
            {
                var errors = EnrollmentRequestValidator.ValidateCreate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var result = await service.CreateAsync(courseId, request, ct);
                return result.ToCreatedResult(e => $"/api/enrollments/{e.Id}");
            })
            .RequireAuthorization("TeacherOnly");

        var enrollments = app.MapGroup("/api/enrollments").WithTags("Enrollments");

        enrollments.MapGet("/{id:int}", async (int id, IEnrollmentService service, CancellationToken ct) =>
                (await service.GetByIdAsync(id, ct)).ToHttpResult())
            .RequireAuthorization("TeacherOnly");

        enrollments.MapPut("/{id:int}/mark", async (int id, MarkRequest request,
                IEnrollmentService service, CancellationToken ct) =>
            {
                var errors = EnrollmentRequestValidator.ValidateMark(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                return (await service.SetMarkAsync(id, request, ct)).ToHttpResult();
            })
            .RequireAuthorization("TeacherOnly");

        enrollments.MapDelete("/{id:int}", async (int id, IEnrollmentService service, CancellationToken ct) =>
                (await service.DeleteAsync(id, ct)).ToNoContentResult())
            .RequireAuthorization("TeacherOnly");

        return app;
    }
}