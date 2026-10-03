using Microsoft.AspNetCore.Mvc;
using Products.Application.Dtos.Users;
using Products.Application.Services;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(UserService userService) : ControllerBase
{
    /// <summary>Selects a user by email (no password, see README) and records the login.</summary>
    [HttpPost("login")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UserResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        userService.LoginAsync(request.Email, cancellationToken);
}
