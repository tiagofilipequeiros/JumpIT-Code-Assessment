using Microsoft.AspNetCore.Mvc;
using Products.Application.Dtos.Users;
using Products.Application.Services;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(UserService userService) : ControllerBase
{
    /// <summary>The users available in the user dropdown.</summary>
    [HttpGet]
    public Task<List<UserResponse>> GetAll(CancellationToken cancellationToken) =>
        userService.GetAllAsync(cancellationToken);
}
