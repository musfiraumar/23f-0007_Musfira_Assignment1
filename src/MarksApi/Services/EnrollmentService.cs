using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos.Common;
using MarksApi.Dtos.Enrollments;
using MarksApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class EnrollmentService(AppDbContext db, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<ServiceResult<PagedResult<CourseEnrollmentResponse>>> ListByCourseAsync(
        int courseId, int page, int pageSize, string? search, string? sort, CancellationToken ct)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, ct))
            return ServiceResult<PagedResult<CourseEnrollmentResponse>>.NotFound("Course not found");

        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Student.FullName.ToLower().Contains(term) ||
                e.Student.RegistrationNo.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var desc = sort?.StartsWith('-') == true;
        var key = sort?.TrimStart('-').ToLowerInvariant();
        IOrderedQueryable<Enrollment> ordered = key switch
        {
            "fullname" => desc ? query.OrderByDescending(e => e.Student.FullName) : query.OrderBy(e => e.Student.FullName),
            "registrationno" => desc ? query.OrderByDescending(e => e.Student.RegistrationNo) : query.OrderBy(e => e.Student.RegistrationNo),
            "mark" => desc ? query.OrderByDescending(e => e.Mark) : query.OrderBy(e => e.Mark),
            _ => desc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new CourseEnrollmentResponse(
                e.Id, e.StudentId, e.Student.RegistrationNo, e.Student.FullName, e.Mark, e.EnrolledAtUtc))
            .ToListAsync(ct);

        return ServiceResult<PagedResult<CourseEnrollmentResponse>>.Ok(
            new PagedResult<CourseEnrollmentResponse>(items, page, pageSize, totalCount));
    }

    public async Task<ServiceResult<EnrollmentResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EnrollmentResponse(e.Id, e.StudentId, e.CourseId, e.Mark))
            .FirstOrDefaultAsync(ct);

        return enrollment is null
            ? ServiceResult<EnrollmentResponse>.NotFound("Enrollment not found")
            : ServiceResult<EnrollmentResponse>.Ok(enrollment);
    }

    public async Task<ServiceResult<EnrollmentResponse>> CreateAsync(
        int courseId, EnrollmentCreateRequest request, CancellationToken ct)
    {
        var studentId = request.StudentId!.Value;

        if (!await db.Courses.AnyAsync(c => c.Id == courseId, ct))
            return ServiceResult<EnrollmentResponse>.NotFound("Course not found");
        if (!await db.Students.AnyAsync(s => s.Id == studentId, ct))
            return ServiceResult<EnrollmentResponse>.NotFound("Student not found");

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAtUtc = DateTime.UtcNow
        };

        db.Enrollments.Add(enrollment);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            logger.LogWarning("Duplicate enrollment: student {StudentId} in course {CourseId}", studentId, courseId);
            return ServiceResult<EnrollmentResponse>.Conflict(
                "Duplicate enrollment", "This student is already enrolled in this course.");
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            // The student or course was deleted between the check and the insert
            return ServiceResult<EnrollmentResponse>.NotFound("Student or course not found");
        }

        logger.LogInformation("Enrollment created: {EnrollmentId} student {StudentId} course {CourseId}",
            enrollment.Id, studentId, courseId);
        return ServiceResult<EnrollmentResponse>.Ok(
            new EnrollmentResponse(enrollment.Id, studentId, courseId, enrollment.Mark));
    }

    public async Task<ServiceResult<EnrollmentResponse>> SetMarkAsync(
        int id, MarkRequest request, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null)
            return ServiceResult<EnrollmentResponse>.NotFound("Enrollment not found");

        var oldMark = enrollment.Mark;
        enrollment.Mark = request.Mark!.Value;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Mark changed: enrollment {EnrollmentId} student {StudentId} course {CourseId} from {OldMark} to {NewMark}",
            id, enrollment.StudentId, enrollment.CourseId, oldMark, enrollment.Mark);
        return ServiceResult<EnrollmentResponse>.Ok(
            new EnrollmentResponse(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Mark));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null)
            return ServiceResult<bool>.NotFound("Enrollment not found");

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment deleted: {EnrollmentId}", id);
        return ServiceResult<bool>.Ok(true);
    }
}