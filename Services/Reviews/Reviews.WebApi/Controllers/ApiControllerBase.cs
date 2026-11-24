using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    // Ідентифікатор і ім'я автора беруться з claims токена (sub, unique_name), а не з тіла запиту,
    // тому користувач не може створити відгук чи коментар від чужого імені.
    protected string CurrentUserId =>
        User.FindFirstValue("sub")
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Token has no 'sub' claim");

    protected string CurrentUserName =>
        User.FindFirstValue("unique_name")
        ?? User.Identity?.Name
        ?? CurrentUserId;
}