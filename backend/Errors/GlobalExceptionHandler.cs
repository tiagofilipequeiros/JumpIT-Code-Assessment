using Microsoft.AspNetCore.Diagnostics;

namespace Backend.Errors;

// Turns every exception into a ProblemDetails response. Only unexpected errors are logged.
public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var appException = exception as AppException;
        var code = appException?.Code ?? ErrorCode.Unexpected;

        if (appException is null)
        {
            logger.LogError(exception, "Unexpected error on {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = code.Status();

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = code.Status(),
                Title = code.Message(),
                Detail = appException?.Detail,
                Extensions = { ["code"] = code.ToString() },
            },
        });
    }
}
