using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Services;
using Products.Domain.Authorization;
using Products.Domain.Entities;
using Products.Domain.Errors;
using Products.UnitTests.Fakes;

namespace Products.UnitTests.Services;

public class CurrentUserTests
{
    [Fact]
    public async Task Anonymous_requests_have_no_permissions()
    {
        Assert.False(await TestData.CurrentUser(role: null).HasAsync(Permission.ChangeStock, default));
    }

    [Fact]
    public async Task Unknown_user_id_is_treated_as_anonymous()
    {
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.UserId.Returns(999);
        var currentUser = new CurrentUser(accessor, Substitute.For<IUserRepository>());

        var error = await Assert.ThrowsAsync<AppException>(() => currentUser.RequireAsync(Permission.ChangeStock, default));

        Assert.Equal(ErrorCode.UserRequired, error.Code);
    }

    [Fact]
    public async Task The_user_is_loaded_once_per_request()
    {
        var accessor = Substitute.For<ICurrentUserAccessor>();
        var users = Substitute.For<IUserRepository>();
        accessor.UserId.Returns(1);
        users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new User { Id = 1, Role = Role.Admin });
        var currentUser = new CurrentUser(accessor, users);

        await currentUser.HasAsync(Permission.Edit, default);
        await currentUser.RequireAsync(Permission.Delete, default);

        await users.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }
}
