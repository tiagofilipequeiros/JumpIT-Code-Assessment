using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Services;
using Products.Domain.Entities;

namespace Products.UnitTests.Fakes;

public static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public static readonly Category Objectives = new() { Id = 2, Name = "Objectives", IsActive = true };

    public static Product Product(int id = 100000, int stock = 10, decimal price = 10m) => new()
    {
        Id = id,
        Name = "Lens",
        Price = price,
        Stock = stock,
        CategoryId = Objectives.Id,
        Category = Objectives,
        IsActive = true,
        RowVersion = [1, 2, 3],
    };

    // A CurrentUser that resolves to a user with the given role (or to nobody).
    public static CurrentUser CurrentUser(Role? role)
    {
        var accessor = Substitute.For<ICurrentUserAccessor>();
        var users = Substitute.For<IUserRepository>();
        if (role is { } value)
        {
            accessor.UserId.Returns(7);
            users.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new User { Id = 7, Name = $"Test {value}", Role = value });
        }

        return new CurrentUser(accessor, users);
    }
}
