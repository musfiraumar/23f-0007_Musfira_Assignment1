using System.Security.Claims;
using MarksApi.Data;
using MarksApi.Dtos.Reports;
using MarksApi.Infrastructure;
using MarksApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarksApi.Services;

public class ReportService(
    AppDbContext db,
    IOptions<MarkingOptions> marking,
    ILogger<ReportService> logger) : IReportService
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

        var response = agg is null
            ? new CourseStatsResponse(course.Id, course.Code, 0, 0, null, null, null, 0)
            : new CourseStatsResponse(
                course.Id, course.Code, agg.Enrolled, agg.Graded,
                agg.Average is null ? null : Math.Round(agg.Average.Value, 2),
                agg.Highest, agg.Lowest, agg.Passed);

        return ServiceResult<CourseStatsResponse>.Ok(response);
    }

    public async Task<ServiceResult<TranscriptResponse>> GetTranscriptAsync(
        int studentId, ClaimsPrincipal caller, CancellationToken ct)
    {
        // Ownership rule first, so a student can't learn which ids exist
        if (!caller.IsTeacher() && caller.GetStudentId() != studentId)
        {
            logger.LogWarning("Forbidden transcript read: student {CallerId} tried to read {StudentId}",
                caller.GetStudentId(), studentId);
            return ServiceResult<TranscriptResponse>.Forbidden("You can only view your own transcript.");
        }

        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.Id, s.RegistrationNo, s.FullName })
            .FirstOrDefaultAsync(ct);

        if (student is null)
            return ServiceResult<TranscriptResponse>.NotFound("Student not found");

        var passMark = marking.Value.PassMark;

        var rows = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Course.Code)
            .Select(e => new
            {
                e.CourseId,
                CourseCode = e.Course.Code,
                e.Course.Title,
                e.Course.CreditHours,
                e.Mark
            })
            .ToListAsync(ct);

        var courses = rows
            .Select(r => new TranscriptCourseResponse(
                r.CourseId, r.CourseCode, r.Title, r.CreditHours, r.Mark,
                r.Mark is null ? null : r.Mark >= passMark))
            .ToList();

        // Weighted average = sum(mark x credit hours) / sum(credit hours), graded courses only
        var graded = rows.Where(r => r.Mark is not null).ToList();
        var gradedCredits = graded.Sum(r => r.CreditHours);
        double? weighted = gradedCredits == 0
            ? null
            : Math.Round(graded.Sum(r => (double)r.Mark!.Value * r.CreditHours) / gradedCredits, 2);

        return ServiceResult<TranscriptResponse>.Ok(new TranscriptResponse(
            student.Id, student.RegistrationNo, student.FullName, courses, gradedCredits, weighted));
    }
}