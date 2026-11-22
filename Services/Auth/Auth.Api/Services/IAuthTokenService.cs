using Auth.Api.Data;

namespace Auth.Api.Services;

public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public interface IAuthTokenService
{
    Task<AuthTokens> IssueAsync(ApplicationUser user, string? ipAddress, CancellationToken ct = default);

    Task<AuthTokens?> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);

    Task<bool> RevokeAsync(string refreshToken, string reason, string? ownerUserId = null, CancellationToken ct = default);

    Task RevokeAllAsync(string userId, string reason, CancellationToken ct = default);
}