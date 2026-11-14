using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarksApi.Infrastructure;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        string title;
        string detail;

        if (exception is BadHttpRequestException bad)
        {
            status = bad.StatusCode;
            title = "Bad request";
            detail = "The request body or parameters could not be read.";
            logger.LogWarning("Bad request on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, bad.Message);
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            title = "An unexpected error occurred.";
            detail = "Please try again later.";
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}