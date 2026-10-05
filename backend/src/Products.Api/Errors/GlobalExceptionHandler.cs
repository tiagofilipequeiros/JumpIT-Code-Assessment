using Microsoft.AspNetCore.Diagnostics;
using Products.Domain.Errors;

namespace Products.Api.Errors;

// Turns every exception into a ProblemDetails response. Only unexpected errors are logged.
public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // The client went away: there is nobody to answer and nothing failed on our side.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var appException = exception as AppException;
        var (code, status) = exception switch
        {
            AppException app => (app.Code, app.Code.Status()),
            // Malformed requests rejected by the framework itself (e.g. body too large) keep their status.
            BadHttpRequestException bad => (ErrorCodeMappings.FromStatus(bad.StatusCode), bad.StatusCode),
            _ => (ErrorCode.Unexpected, StatusCodes.Status500InternalServerError),
        };

        if (code == ErrorCode.Unexpected)
        {
            logger.LogError(exception, "Unexpected error on {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        var problem = new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = code.Message(),
                Detail = appException?.Detail,
                Extensions = { ["code"] = code.ToString() },
            },
        };

        // Same shape as model validation errors: { "errors": { "quantity": ["..."] } }.
        if (appException?.Field is { } field)
        {
            problem.ProblemDetails.Extensions["errors"] = new Dictionary<string, string[]>
            {
                [field] = [appException.Detail ?? code.Message()],
            };
        }

        return await problemDetailsService.TryWriteAsync(problem);
    }
}
