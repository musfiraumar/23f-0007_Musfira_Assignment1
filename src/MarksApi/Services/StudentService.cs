using System.Security.Claims;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos.Common;
using MarksApi.Dtos.Students;
using MarksApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class StudentService(AppDbContext db, ILogger<StudentService> logger) : IStudentService
{
    private const string DuplicateTitle = "Duplicate student";
    private const string DuplicateDetail = "A student with this registration number or email already exists.";

    public async Task<PagedResult<StudentResponse>> ListAsync(
        int page, int pageSize, string? search, string? sort, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(term) ||
                s.RegistrationNo.ToLower().Contains(term) ||
                s.Email.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var desc = sort?.StartsWith('-') == true;
        var key = sort?.TrimStart('-').ToLowerInvariant();
        IOrderedQueryable<Student> ordered = key switch
        {
            "fullname" => desc ? query.OrderByDescending(s => s.FullName) : query.OrderBy(s => s.FullName),
            "registrationno" => desc ? query.OrderByDescending(s => s.RegistrationNo) : query.OrderBy(s => s.RegistrationNo),
            "email" => desc ? query.OrderByDescending(s => s.Email) : query.OrderBy(s => s.Email),
            _ => desc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id)
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentResponse(s.Id, s.RegistrationNo, s.FullName, s.Email))
            .ToListAsync(ct);

        return new PagedResult<StudentResponse>(items, page, pageSize, totalCount);
    }

    public async Task<ServiceResult<StudentResponse>> GetByIdAsync(
        int id, ClaimsPrincipal caller, CancellationToken ct)
    {
        // Ownership rule first, so a student can't learn which ids exist
        if (!caller.IsTeacher() && caller.GetStudentId() != id)
        {
            logger.LogWarning("Forbidden student read: student {CallerId} tried to read {StudentId}",
                caller.GetStudentId(), id);
            return ServiceResult<StudentResponse>.Forbidden("You can only view your own record.");
        }

        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentResponse(s.Id, s.RegistrationNo, s.FullName, s.Email))
            .FirstOrDefaultAsync(ct);

        return student is null
            ? ServiceResult<StudentResponse>.NotFound("Student not found")
            : ServiceResult<StudentResponse>.Ok(student);
    }

    public async Task<ServiceResult<StudentResponse>> CreateAsync(StudentRequest request, CancellationToken ct)
    {
        var student = new Student
        {
            RegistrationNo = request.RegistrationNo!.Trim(),
            FullName = request.FullName!.Trim(),
            Email = request.Email!.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Students.Add(student);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            logger.LogWarning("Duplicate student on create: {RegistrationNo}", student.RegistrationNo);
            return ServiceResult<StudentResponse>.Conflict(DuplicateTitle, DuplicateDetail);
        }

        logger.LogInformation("Student created: {StudentId} {RegistrationNo}", student.Id, student.RegistrationNo);
        return ServiceResult<StudentResponse>.Ok(ToResponse(student));
    }

    public async Task<ServiceResult<StudentResponse>> UpdateAsync(
        int id, StudentRequest request, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null)
            return ServiceResult<StudentResponse>.NotFound("Student not found");

        student.RegistrationNo = request.RegistrationNo!.Trim();
        student.FullName = request.FullName!.Trim();
        student.Email = request.Email!.Trim().ToLowerInvariant();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            logger.LogWarning("Duplicate student on update: {StudentId}", id);
            return ServiceResult<StudentResponse>.Conflict(DuplicateTitle, DuplicateDetail);
        }

        logger.LogInformation("Student updated: {StudentId}", id);
        return ServiceResult<StudentResponse>.Ok(ToResponse(student));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null)
            return ServiceResult<bool>.NotFound("Student not found");

        db.Students.Remove(student);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            logger.LogWarning("Delete blocked, student {StudentId} still referenced", id);
            return ServiceResult<bool>.Conflict(
                "Student cannot be deleted",
                "The student still has enrollments.");
        }

        logger.LogInformation("Student deleted: {StudentId}", id);
        return ServiceResult<bool>.Ok(true);
    }

    private static StudentResponse ToResponse(Student s) =>
        new(s.Id, s.RegistrationNo, s.FullName, s.Email);
}