using System.Security.Claims;
using MarksApi.Infrastructure;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Reports");

        group.MapGet("/courses/{courseId:int}/stats",
                async (int courseId, IReportService service, CancellationToken ct) =>
                    (await service.GetCourseStatsAsync(courseId, ct)).ToHttpResult())
            .RequireAuthorization("TeacherOnly");

        group.MapGet("/students/{id:int}/transcript",
                async (int id, ClaimsPrincipal user, IReportService service, CancellationToken ct) =>
                    (await service.GetTranscriptAsync(id, user, ct)).ToHttpResult())
            .RequireAuthorization();

        return app;
    }
}