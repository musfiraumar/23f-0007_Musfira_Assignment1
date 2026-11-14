using System.Security.Claims;
using MarksApi.Dtos.Common;
using MarksApi.Dtos.Students;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<PagedResult<StudentResponse>> ListAsync(
        int page, int pageSize, string? search, string? sort, CancellationToken ct);
    Task<ServiceResult<StudentResponse>> GetByIdAsync(int id, ClaimsPrincipal caller, CancellationToken ct);
    Task<ServiceResult<StudentResponse>> CreateAsync(StudentRequest request, CancellationToken ct);
    Task<ServiceResult<StudentResponse>> UpdateAsync(int id, StudentRequest request, CancellationToken ct);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken ct);
}