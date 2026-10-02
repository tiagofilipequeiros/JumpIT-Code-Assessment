using Products.Application.Abstractions;
using Products.Domain.Authorization;
using Products.Domain.Entities;
using Products.Domain.Errors;

namespace Products.Application.Services;

// The user making the request. Deliberately simple (no real authentication): the assessment only needs "who did what".
public class CurrentUser(ICurrentUserAccessor accessor, IUserRepository users)
{
    private User? _user;
    private bool _loaded;

    public async Task<User?> GetAsync(CancellationToken cancellationToken)
    {
        if (!_loaded)
        {
            _loaded = true;
            _user = accessor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
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
