using Products.Domain.Authorization;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Users;

public record UserResponse(int Id, string Name, string Email, Role Role, IReadOnlyCollection<Permission> Permissions)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, Domain.Authorization.Permissions.For(user.Role));
}
