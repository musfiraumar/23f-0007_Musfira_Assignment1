using MarksApi.Dtos.Common;
using MarksApi.Dtos.Enrollments;

namespace MarksApi.Services;

public interface IEnrollmentService
{
    Task<ServiceResult<PagedResult<CourseEnrollmentResponse>>> ListByCourseAsync(
        int courseId, int page, int pageSize, string? search, string? sort, CancellationToken ct);
    Task<ServiceResult<EnrollmentResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<EnrollmentResponse>> CreateAsync(
        int courseId, EnrollmentCreateRequest request, CancellationToken ct);
    Task<ServiceResult<EnrollmentResponse>> SetMarkAsync(int id, MarkRequest request, CancellationToken ct);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct);
}