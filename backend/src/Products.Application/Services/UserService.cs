using Products.Application.Abstractions;
using Products.Application.Dtos.Users;
using Products.Domain.Entities;
using Products.Domain.Errors;

namespace Products.Application.Services;

public class UserService(IUserRepository users, IUnitOfWork unitOfWork, MetricsService metrics)
{
    public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await users.ListAsync(cancellationToken)).Select(UserResponse.From).ToList();

    // "Login" = pick a user by email. No password on purpose; it only identifies who does what.
    public async Task<UserResponse> LoginAsync(string email, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(email.Trim(), cancellationToken)
            ?? throw new AppException(ErrorCode.UserNotFound, $"No user with email '{email}'.");

        metrics.Record(user.Id, MetricEntity.User, MetricAction.Login, user.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }
}
