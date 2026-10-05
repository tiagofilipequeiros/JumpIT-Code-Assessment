using System.ComponentModel.DataAnnotations;
using Products.Application.Validation;

namespace Products.Application.Dtos.Users;

// Body of POST /api/auth/login. No password on purpose (see README).
public class LoginRequest
{
    private readonly string _email = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get => _email; init => _email = TextInput.Trim(value); }
}
