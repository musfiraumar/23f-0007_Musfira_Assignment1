using MarksApi.Dtos.Common;
using MarksApi.Dtos.Courses;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> ListAsync(
        int page, int pageSize, string? search, string? sort, CancellationToken ct);
    Task<ServiceResult<CourseResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<CourseResponse>> CreateAsync(CourseRequest request, CancellationToken ct);
    Task<ServiceResult<CourseResponse>> UpdateAsync(int id, CourseRequest request, CancellationToken ct);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct);
}