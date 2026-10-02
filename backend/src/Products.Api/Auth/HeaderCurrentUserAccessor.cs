using Products.Application.Abstractions;

namespace Products.Api.Auth;

// Reads the current user's ID from the X-User-Id header. Real authentication would read it from the token claims instead.
public class HeaderCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public const string HeaderName = "X-User-Id";

    public int? UserId =>
        int.TryParse(httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString(), out var id) ? id : null;
}
