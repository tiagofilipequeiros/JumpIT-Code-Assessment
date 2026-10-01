using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Produces("application/json")]
public class UsersController(UserService userService) : ControllerBase
{
    /// <summary>The users available in the user dropdown.</summary>
    [HttpGet("api/users")]
    public Task<List<UserResponse>> GetAll(CancellationToken cancellationToken) =>
        userService.GetAllAsync(cancellationToken);

    /// <summary>Selects a user by email (no password) and records the login.</summary>
    [HttpPost("api/auth/login")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UserResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        userService.LoginAsync(request.Email, cancellationToken);
}
