using System.ComponentModel.DataAnnotations;

namespace Products.Application.Dtos.Users;

// Body of POST /api/auth/login. No password on purpose (see README).
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
