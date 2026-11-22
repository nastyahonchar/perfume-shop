using System.Security.Claims;
using Auth.Api.Data;
using Auth.Api.Dtos;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthTokenService tokenService,
    IAuthEmailService emailService,
    IWebHostEnvironment environment,
    ILogger<AuthController> logger) : ControllerBase
{
    public const string RefreshCookieName = "refreshToken";

    // Реєстрація створює користувача через UserManager.CreateAsync, автоматично призначає роль User,
    // генерує токен підтвердження email і надсилає лист (у розробці це mock-сервіс, що пише лист у лог).
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await userManager.AddToRoleAsync(user, AppRoles.User);

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user, confirmationToken, ct);

        logger.LogInformation("User {UserId} registered with role {Role}", user.Id, AppRoles.User);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(
            user.Id,
            user.UserName!,
            user.Email!,
            "Registration completed. Confirm your email to sign in.",
            environment.IsDevelopment() ? confirmationToken : null));
    }

    // Login перевіряє пароль із підрахунком невдалих спроб (lockout). Користувач без підтвердженого email
    // не входить (403). Access token повертається в тілі відповіді, refresh token лише в HttpOnly cookie.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(request.UserNameOrEmail)
                   ?? await userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsNotAllowed)
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Email is not confirmed");

        if (result.IsLockedOut)
        {
            logger.LogWarning("Login rejected: user {UserId} is locked out", user.Id);
            return Problem(statusCode: StatusCodes.Status423Locked, title: "Account is locked out. Try again later");
        }

        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");

        var tokens = await tokenService.IssueAsync(user, ClientIp(), ct);
        SetRefreshCookie(tokens);

        logger.LogInformation("User {UserId} signed in", user.Id);
        return Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token is missing");

        var tokens = await tokenService.RefreshAsync(refreshToken, ClientIp(), ct);
        if (tokens is null)
        {
            DeleteRefreshCookie();
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Refresh token is invalid, expired or revoked");
        }

        SetRefreshCookie(tokens);
        return Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RevokeTokenRequest? request,
        CancellationToken ct)
    {
        var token = request?.Token ?? Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(token))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Token is required");

        var revoked = await tokenService.RevokeAsync(token, "Revoked by user", CurrentUserId(), ct);
        if (!revoked)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Token not found or already revoked");

        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(token))
            await tokenService.RevokeAsync(token, "Logout", CurrentUserId(), ct);

        DeleteRefreshCookie();
        logger.LogInformation("User {UserId} signed out", CurrentUserId());
        return NoContent();
    }

    // Refresh token передається лише в HttpOnly cookie (недоступний для JavaScript, захист від XSS),
    // SameSite=Strict захищає від CSRF, Secure вимикається тільки в розробці. Cookie діє лише на шляху /auth.
    private void SetRefreshCookie(AuthTokens tokens)
    {
        Response.Cookies.Append(RefreshCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(tokens.RefreshTokenExpiresAt, DateTimeKind.Utc)),
            Path = "/auth"
        });
    }

    private void DeleteRefreshCookie()
        => Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/auth" });

    private string? CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}