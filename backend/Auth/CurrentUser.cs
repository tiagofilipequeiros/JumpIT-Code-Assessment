using Backend.Data;
using Backend.Errors;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Auth;

// The user making the request, taken from the X-User-Id header.
// Deliberately simple (no real authentication): the assessment only needs "who did what".
public class CurrentUser(IHttpContextAccessor httpContextAccessor, AppDbContext db)
{
    public const string HeaderName = "X-User-Id";

    private User? _user;
    private bool _loaded;

    public async Task<User?> GetAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return _user;
        }

        _loaded = true;
        var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
        if (int.TryParse(header, out var userId))
        {
            _user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        return _user;
    }

    // Anonymous requests can read like a normal user.
    public async Task<bool> HasAsync(Permission permission, CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        return user is not null && user.Role.Has(permission);
    }

    public async Task<User> RequireAsync(Permission permission, CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken)
            ?? throw new AppException(ErrorCode.UserRequired);

        if (!user.Role.Has(permission))
        {
            throw new AppException(ErrorCode.Forbidden, $"Role {user.Role} cannot perform this action.");
        }

        return user;
    }
}
