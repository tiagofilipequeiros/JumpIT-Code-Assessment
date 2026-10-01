using Backend.Data;
using Backend.Dtos;
using Backend.Errors;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class UserService(AppDbContext db, MetricsService metrics)
{
    public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);
        return users.Select(UserResponse.From).ToList();
    }

    // "Login" = pick a user by email. No password on purpose; it only identifies who does what.
    public async Task<UserResponse> LoginAsync(string email, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email.Trim(), cancellationToken)
            ?? throw new AppException(ErrorCode.UserNotFound, $"No user with email '{email}'.");

        metrics.Record(user.Id, MetricEntity.User, MetricAction.Login, user.Id);
        await db.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }
}
