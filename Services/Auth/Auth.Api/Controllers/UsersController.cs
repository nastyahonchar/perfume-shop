using System.Security.Claims;
using Auth.Api.Data;
using Auth.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db,
    ILogger<UsersController> logger) : ControllerBase
{
    private const int MaxPageSize = 100;

    // Список користувачів, видалення й керування ролями доступні лише ролі Admin: перевірка відбувається
    // за claim role в JWT, без звернення до БД. Ендпоінти користувача (дані, профіль, історія токенів)
    // дозволені власнику або Admin: userId з токена (sub) порівнюється з {id} у маршруті.
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PagedUsersResponse>> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await userManager.Users.CountAsync(ct);
        var users = await userManager.Users
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ids = users.Select(u => u.Id).ToList();
        var userRoles = await (
                from ur in db.UserRoles
                join role in db.Roles on ur.RoleId equals role.Id
                where ids.Contains(ur.UserId)
                select new { ur.UserId, RoleName = role.Name })
            .ToListAsync(ct);

        var items = users
            .Select(u => ToDto(
                u,
                userRoles.Where(r => r.UserId == u.Id).Select(r => r.RoleName!).OrderBy(r => r).ToList()))
            .ToList();

        return Ok(new PagedUsersResponse(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        return Ok(ToDto(user, await GetRolesAsync(user)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            var nameResult = await userManager.SetUserNameAsync(user, request.UserName.Trim());
            if (!nameResult.Succeeded)
                return IdentityValidationProblem(nameResult);
        }

        if (request.PhoneNumber is not null && request.PhoneNumber != user.PhoneNumber)
        {
            var phoneResult = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!phoneResult.Succeeded)
                return IdentityValidationProblem(phoneResult);
        }

        logger.LogInformation("User {UserId} profile updated by {ActorId}", id, CurrentUserId());
        return Ok(ToDto(user, await GetRolesAsync(user)));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (id == CurrentUserId())
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "You cannot delete your own account");

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        logger.LogInformation("User {UserId} deleted by admin {ActorId}", id, CurrentUserId());
        return NoContent();
    }

    [HttpGet("{id}/refresh-tokens")]
    public async Task<IActionResult> GetRefreshTokens(string id, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return Ok(tokens.Select(t => new RefreshTokenDto(
            t.Id, t.CreatedAt, t.ExpiresAt, t.RevokedAt, t.RevokedReason, t.CreatedByIp, t.IsActive)));
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        return Ok(await GetRolesAsync(user));
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        if (!await roleManager.RoleExistsAsync(request.Role))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Role '{request.Role}' does not exist");

        if (await userManager.IsInRoleAsync(user, request.Role))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "User already has this role");

        var result = await userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        logger.LogInformation("Role {Role} assigned to user {UserId} by admin {ActorId}", request.Role, id, CurrentUserId());
        return Ok(await GetRolesAsync(user));
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFoundProblem();

        if (!await userManager.IsInRoleAsync(user, role))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "User does not have this role");

        if (string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            && (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count <= 1)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Cannot remove the last administrator");
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        logger.LogInformation("Role {Role} removed from user {UserId} by admin {ActorId}", role, id, CurrentUserId());
        return NoContent();
    }

    private bool IsOwnerOrAdmin(string id)
        => User.IsInRole(AppRoles.Admin) || string.Equals(CurrentUserId(), id, StringComparison.Ordinal);

    private string? CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private async Task<List<string>> GetRolesAsync(ApplicationUser user)
        => (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();

    private static UserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles) => new(
        user.Id, user.UserName!, user.Email!, user.EmailConfirmed, user.PhoneNumber,
        user.CreatedAt, user.LockoutEnd, roles);

    private IActionResult NotFoundProblem()
        => Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found");

    private IActionResult IdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Code, error.Description);
        return ValidationProblem(ModelState);
    }
}