namespace Products.Domain.Errors;

// An expected error (not found, not enough stock, ...). Returned to the client, never logged.
// Field: the input the error is about (e.g. "quantity"); sent like model validation errors, per field.
public class AppException(ErrorCode code, string? detail = null, string? field = null) : Exception(detail ?? code.ToString())
{
    public ErrorCode Code { get; } = code;
    public string? Detail { get; } = detail;
    public string? Field { get; } = field;
}
