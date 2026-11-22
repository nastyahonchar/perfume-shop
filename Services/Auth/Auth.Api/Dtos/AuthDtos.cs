using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class RevokeTokenRequest
{
    public string? Token { get; set; }
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");

public sealed record RegisterResponse(
    string UserId,
    string UserName,
    string Email,
    string Message,
    string? EmailConfirmationToken);