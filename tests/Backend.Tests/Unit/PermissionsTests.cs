using Backend.Auth;
using Backend.Models;

namespace Backend.Tests.Unit;

public class PermissionsTests
{
    [Theory]
    [InlineData(Role.User, Permission.ChangeStock, true)]
    [InlineData(Role.User, Permission.Edit, false)]
    [InlineData(Role.User, Permission.ToggleActive, false)]
    [InlineData(Role.User, Permission.ViewHidden, false)]
    [InlineData(Role.User, Permission.Delete, false)]
    [InlineData(Role.User, Permission.ViewMetrics, false)]
    [InlineData(Role.Editor, Permission.ChangeStock, true)]
    [InlineData(Role.Editor, Permission.Edit, true)]
    [InlineData(Role.Editor, Permission.ToggleActive, true)]
    [InlineData(Role.Editor, Permission.ViewHidden, true)]
    [InlineData(Role.Editor, Permission.Delete, false)]
    [InlineData(Role.Editor, Permission.ViewMetrics, false)]
    public void Roles_have_the_agreed_permissions(Role role, Permission permission, bool expected)
    {
        Assert.Equal(expected, role.Has(permission));
    }

    [Fact]
    public void Admin_has_every_permission()
    {
        Assert.All(Enum.GetValues<Permission>(), permission => Assert.True(Role.Admin.Has(permission)));
    }
}
