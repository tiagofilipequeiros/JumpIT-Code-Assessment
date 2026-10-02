using Products.Domain.Errors;

namespace Products.Api.Errors;

// The HTTP status and default message for each error code.
public static class ErrorCodeMappings
{
    private static readonly Dictionary<ErrorCode, (int Status, string Message)> Definitions = new()
    {
        [ErrorCode.Unexpected] = (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        [ErrorCode.RequestFailed] = (StatusCodes.Status400BadRequest, "The request could not be processed."),
        [ErrorCode.ValidationFailed] = (StatusCodes.Status400BadRequest, "One or more fields are invalid."),
        [ErrorCode.NotFound] = (StatusCodes.Status404NotFound, "The requested resource was not found."),
        [ErrorCode.UserRequired] = (StatusCodes.Status401Unauthorized, "A valid X-User-Id header is required."),
        [ErrorCode.UserNotFound] = (StatusCodes.Status404NotFound, "User not found."),
        [ErrorCode.Forbidden] = (StatusCodes.Status403Forbidden, "You do not have permission to perform this action."),
        [ErrorCode.ConcurrencyConflict] = (StatusCodes.Status409Conflict, "This item was changed by someone else. Reload and try again."),
        [ErrorCode.ProductNotFound] = (StatusCodes.Status404NotFound, "Product not found."),
        [ErrorCode.InsufficientStock] = (StatusCodes.Status409Conflict, "Not enough stock."),
        [ErrorCode.StockLimitExceeded] = (StatusCodes.Status409Conflict, "Stock would exceed the maximum allowed."),
        [ErrorCode.InvalidQuantity] = (StatusCodes.Status400BadRequest, "Quantity is out of the allowed range."),
        [ErrorCode.InvalidStockRange] = (StatusCodes.Status400BadRequest, "Minimum stock cannot be greater than maximum stock."),
        [ErrorCode.CategoryNotFound] = (StatusCodes.Status404NotFound, "Category not found."),
        [ErrorCode.InvalidCategory] = (StatusCodes.Status400BadRequest, "The selected category cannot be used."),
        [ErrorCode.CategoryNameTaken] = (StatusCodes.Status409Conflict, "A category with this name already exists."),
        [ErrorCode.CategoryProtected] = (StatusCodes.Status409Conflict, "The Uncategorized category cannot be changed."),
    };

    public static int Status(this ErrorCode code) => Definitions[code].Status;

    public static string Message(this ErrorCode code) => Definitions[code].Message;

    // For errors produced by ASP.NET Core itself (unknown route, invalid JSON, ...).
    public static ErrorCode FromStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ErrorCode.ValidationFailed,
        StatusCodes.Status404NotFound => ErrorCode.NotFound,
        >= StatusCodes.Status500InternalServerError => ErrorCode.Unexpected,
        _ => ErrorCode.RequestFailed,
    };
}
