using MarksApi.Data;
using MarksApi.Dtos.Reports;
using MarksApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarksApi.Services;

public class ReportService(AppDbContext db, IOptions<MarkingOptions> marking) : IReportService
{
    public async Task<ServiceResult<CourseStatsResponse>> GetCourseStatsAsync(
        int courseId, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.Code })
            .FirstOrDefaultAsync(ct);

        if (course is null)
            return ServiceResult<CourseStatsResponse>.NotFound("Course not found");

        var passMark = marking.Value.PassMark;

        // One SQL query: the database does the counting and averaging.
        // Min, Max and Average ignore NULL marks, so ungraded rows are skipped automatically.
        var agg = await db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .GroupBy(e => 1)
            .Select(g => new
            {
                Enrolled = g.Count(),
                Graded = g.Count(e => e.Mark != null),
                Average = g.Average(e => (double?)e.Mark),
                Highest = g.Max(e => e.Mark),
                Lowest = g.Min(e => e.Mark),
                Passed = g.Count(e => e.Mark != null && e.Mark >= passMark)
            })
            .FirstOrDefaultAsync(ct);

        // No enrollments at all means no group row, so return zeros and nulls.
        var response = agg is null
            ? new CourseStatsResponse(course.Id, course.Code, 0, 0, null, null, null, 0)
            : new CourseStatsResponse(
                course.Id, course.Code, agg.Enrolled, agg.Graded,
                agg.Average is null ? null : Math.Round(agg.Average.Value, 2),
                agg.Highest, agg.Lowest, agg.Passed);

        return ServiceResult<CourseStatsResponse>.Ok(response);
    }
}