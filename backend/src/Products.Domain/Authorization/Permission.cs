using Products.Domain.Entities;

namespace Products.Domain.Authorization;

public enum Permission
{
    ChangeStock,
    Edit,
    ToggleActive,
    ViewHidden,
    Delete,
    ViewMetrics,
}

// The single place that says which role can do what.
public static class Permissions
{
    private static readonly Dictionary<Role, HashSet<Permission>> ByRole = new()
    {
        [Role.User] = [Permission.ChangeStock],
        [Role.Editor] = [Permission.ChangeStock, Permission.Edit, Permission.ToggleActive, Permission.ViewHidden],
        [Role.Admin] = [.. Enum.GetValues<Permission>()],
    };

    public static bool Has(this Role role, Permission permission) => ByRole[role].Contains(permission);

    public static IReadOnlyCollection<Permission> For(Role role) => ByRole[role];
}
