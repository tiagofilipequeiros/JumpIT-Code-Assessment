namespace Products.Domain.Errors;

// Every error the API can return. Sent to clients as text (e.g. "ProductNotFound").
// The HTTP status and message for each code live in the API layer (ErrorCodeMappings).
public enum ErrorCode
{
    Unexpected,
    RequestFailed,
    ValidationFailed,
    NotFound,
    UserRequired,
    UserNotFound,
    Forbidden,
    ConcurrencyConflict,
    ProductNotFound,
    InsufficientStock,
    StockLimitExceeded,
    InvalidQuantity,
    InvalidStockRange,
    CategoryNotFound,
    InvalidCategory,
    CategoryNameTaken,
    CategoryProtected,
    InvalidTimeRange,
}
