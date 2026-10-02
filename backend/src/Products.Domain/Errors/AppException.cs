namespace Products.Domain.Errors;

// An expected error (not found, not enough stock, ...). Returned to the client, never logged.
public class AppException(ErrorCode code, string? detail = null) : Exception(detail ?? code.ToString())
{
    public ErrorCode Code { get; } = code;
    public string? Detail { get; } = detail;
}
