namespace Products.Application.Abstractions;

// Where the current user's ID comes from (the API reads it from the X-User-Id header).
public interface ICurrentUserAccessor
{
    int? UserId { get; }
}
