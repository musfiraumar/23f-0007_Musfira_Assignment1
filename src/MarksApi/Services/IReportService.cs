using MarksApi.Dtos.Reports;

namespace MarksApi.Services;

public interface IReportService
{
    Task<ServiceResult<CourseStatsResponse>> GetCourseStatsAsync(int courseId, CancellationToken ct);
}