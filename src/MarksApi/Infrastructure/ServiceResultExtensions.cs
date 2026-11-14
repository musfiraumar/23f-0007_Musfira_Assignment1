using MarksApi.Services;

namespace MarksApi.Infrastructure;

public static class ServiceResultExtensions
{
    public static IResult ToHttpResult<T>(this ServiceResult<T> r) =>
        r.Status == ResultStatus.Ok ? Results.Ok(r.Value) : r.ToProblem();

    public static IResult ToCreatedResult<T>(this ServiceResult<T> r, Func<T, string> location) =>
        r.Status == ResultStatus.Ok ? Results.Created(location(r.Value!), r.Value) : r.ToProblem();

    public static IResult ToNoContentResult<T>(this ServiceResult<T> r) =>
        r.Status == ResultStatus.Ok ? Results.NoContent() : r.ToProblem();

    private static IResult ToProblem<T>(this ServiceResult<T> r)
    {
        var status = r.Status switch
        {
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Problem(statusCode: status, title: r.Title, detail: r.Detail);
    }
}