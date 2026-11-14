using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos.Common;
using MarksApi.Dtos.Courses;
using MarksApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class CourseService(AppDbContext db, ILogger<CourseService> logger) : ICourseService
{
    private const string DuplicateTitle = "Duplicate course";
    private const string DuplicateDetail = "A course with this code already exists.";

    public async Task<PagedResult<CourseResponse>> ListAsync(
        int page, int pageSize, string? search, string? sort, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Code.ToLower().Contains(term) ||
                c.Title.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var desc = sort?.StartsWith('-') == true;
        var key = sort?.TrimStart('-').ToLowerInvariant();
        IOrderedQueryable<Course> ordered = key switch
        {
            "code" => desc ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "title" => desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "credithours" => desc ? query.OrderByDescending(c => c.CreditHours) : query.OrderBy(c => c.CreditHours),
            _ => desc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseResponse(c.Id, c.Code, c.Title, c.CreditHours))
            .ToListAsync(ct);

        return new PagedResult<CourseResponse>(items, page, pageSize, totalCount);
    }

    public async Task<ServiceResult<CourseResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponse(c.Id, c.Code, c.Title, c.CreditHours))
            .FirstOrDefaultAsync(ct);

        return course is null
            ? ServiceResult<CourseResponse>.NotFound("Course not found")
            : ServiceResult<CourseResponse>.Ok(course);
    }

    public async Task<ServiceResult<CourseResponse>> CreateAsync(CourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code!.Trim().ToUpperInvariant(),
            Title = request.Title!.Trim(),
            CreditHours = request.CreditHours!.Value,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Courses.Add(course);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            logger.LogWarning("Duplicate course on create: {Code}", course.Code);
            return ServiceResult<CourseResponse>.Conflict(DuplicateTitle, DuplicateDetail);
        }

        logger.LogInformation("Course created: {CourseId} {Code}", course.Id, course.Code);
        return ServiceResult<CourseResponse>.Ok(ToResponse(course));
    }

    public async Task<ServiceResult<CourseResponse>> UpdateAsync(
        int id, CourseRequest request, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return ServiceResult<CourseResponse>.NotFound("Course not found");

        course.Code = request.Code!.Trim().ToUpperInvariant();
        course.Title = request.Title!.Trim();
        course.CreditHours = request.CreditHours!.Value;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            logger.LogWarning("Duplicate course on update: {CourseId}", id);
            return ServiceResult<CourseResponse>.Conflict(DuplicateTitle, DuplicateDetail);
        }

        logger.LogInformation("Course updated: {CourseId}", id);
        return ServiceResult<CourseResponse>.Ok(ToResponse(course));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
            return ServiceResult<bool>.NotFound("Course not found");

        db.Courses.Remove(course);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            logger.LogWarning("Delete blocked, course {CourseId} still has enrollments", id);
            return ServiceResult<bool>.Conflict(
                "Course cannot be deleted",
                "The course still has enrollments.");
        }

        logger.LogInformation("Course deleted: {CourseId}", id);
        return ServiceResult<bool>.Ok(true);
    }

    private static CourseResponse ToResponse(Course c) =>
        new(c.Id, c.Code, c.Title, c.CreditHours);
}