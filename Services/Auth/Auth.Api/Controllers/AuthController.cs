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


    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired confirmation token");

        if (user.EmailConfirmed)
            return Ok(new MessageResponse("Email is already confirmed"));

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired confirmation token");

        // ConfirmEmailAsync сам не змінює security stamp, тому робимо це явно: токен стає одноразовим.
        await userManager.UpdateSecurityStampAsync(user);

        logger.LogInformation("Email confirmed for user {UserId}", user.Id);
        return Ok(new MessageResponse("Email confirmed. You can sign in now"));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
    {
        string? devToken = null;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await emailService.SendEmailConfirmationAsync(user, token, ct);
            devToken = environment.IsDevelopment() ? token : null;
        }

        // Відповідь однакова незалежно від того, чи існує акаунт, щоб не розкривати зареєстровані адреси.
        return Ok(new MessageResponse(
            "If the account exists and is not confirmed, a new confirmation email has been sent", devToken));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        string? devToken = null;

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.EmailConfirmed)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailService.SendPasswordResetAsync(user, token, ct);
            devToken = environment.IsDevelopment() ? token : null;
        }

        return Ok(new MessageResponse(
            "If the account exists, a password reset email has been sent", devToken));
    }

    // Скидання пароля працює лише з токеном із листа. Після успішного скидання security stamp змінюється,
    // тому токен вдруге не спрацює, а всі refresh-токени користувача відкликаються (старі сесії завершуються).
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired reset token");

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid or expired reset token");

            return IdentityValidationProblem(result);
        }

        await tokenService.RevokeAllAsync(user.Id, "Password reset", ct);

        logger.LogInformation("Password reset for user {UserId}, all refresh tokens revoked", user.Id);
        return Ok(new MessageResponse("Password has been reset. Sign in with the new password"));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        var user = userId is null ? null : await userManager.FindByIdAsync(userId);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "User not found");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        await tokenService.RevokeAllAsync(user.Id, "Password changed", ct);
        DeleteRefreshCookie();

        logger.LogInformation("Password changed for user {UserId}, all refresh tokens revoked", user.Id);
        return Ok(new MessageResponse("Password has been changed. Sign in again"));
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

    private IActionResult IdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Code, error.Description);
        return ValidationProblem(ModelState);
    }
}