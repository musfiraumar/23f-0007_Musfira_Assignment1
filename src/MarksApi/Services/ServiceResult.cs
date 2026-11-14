namespace MarksApi.Services;

public enum ResultStatus { Ok, NotFound, Conflict, Forbidden }

public sealed record ServiceResult<T>(
    ResultStatus Status, T? Value = default, string? Title = null, string? Detail = null)
{
    public static ServiceResult<T> Ok(T value) => new(ResultStatus.Ok, value);
    public static ServiceResult<T> NotFound(string title, string? detail = null) =>
        new(ResultStatus.NotFound, default, title, detail);
    public static ServiceResult<T> Conflict(string title, string? detail = null) =>
        new(ResultStatus.Conflict, default, title, detail);
    public static ServiceResult<T> Forbidden(string? detail = null) =>
        new(ResultStatus.Forbidden, default, "Forbidden", detail);
}