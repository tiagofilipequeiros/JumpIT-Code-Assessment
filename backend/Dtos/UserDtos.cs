using System.ComponentModel.DataAnnotations;
using Backend.Auth;
using Backend.Models;

namespace Backend.Dtos;

public record UserResponse(int Id, string Name, string Email, Role Role, IReadOnlyCollection<Permission> Permissions)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, Auth.Permissions.For(user.Role));
}

// Body of POST /api/auth/login. No password on purpose (see README).
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
