using System.Security.Claims;
using MarksApi.Dtos.Reports;

namespace MarksApi.Services;

public interface IReportService
{
    Task<ServiceResult<CourseStatsResponse>> GetCourseStatsAsync(int courseId, CancellationToken ct);
    Task<ServiceResult<TranscriptResponse>> GetTranscriptAsync(
        int studentId, ClaimsPrincipal caller, CancellationToken ct);
}