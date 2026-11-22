using Auth.Api.Data;
using Auth.Api.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public sealed class AuthTokenService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IOptions<JwtSettings> options,
    ILogger<AuthTokenService> logger) : IAuthTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public async Task<AuthTokens> IssueAsync(ApplicationUser user, string? ipAddress, CancellationToken ct = default)
    {
        var (tokens, entity) = await CreateAsync(user, ipAddress);
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(ct);
        return tokens;
    }

    // Ротація: при оновленні старий refresh-токен відкликається (RevokedAt, ReplacedByTokenHash), а замість
    // нього створюється новий на 7 днів. Повторне використання вже відкликаного токена означає можливу
    // крадіжку, тому всі активні refresh-токени цього користувача відкликаються.
    public async Task<AuthTokens?> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var hash = RefreshToken.Hash(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            logger.LogWarning("Refresh failed: token not found");
            return null;
        }

        if (stored.IsRevoked)
        {
            logger.LogWarning(
                "Refresh failed: revoked token reused for user {UserId}, all tokens of the user are revoked",
                stored.UserId);
            await RevokeAllAsync(stored.UserId, "Reuse of a revoked refresh token", ct);
            return null;
        }

        if (stored.IsExpired)
        {
            logger.LogWarning("Refresh failed: token expired for user {UserId}", stored.UserId);
            return null;
        }

        var (tokens, replacement) = await CreateAsync(stored.User, ipAddress);

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = "Replaced by new token";
        stored.ReplacedByTokenHash = replacement.TokenHash;

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Refresh token rotated for user {UserId}", stored.UserId);
        return tokens;
    }

    public async Task<bool> RevokeAsync(
        string refreshToken, string reason, string? ownerUserId = null, CancellationToken ct = default)
    {
        var hash = RefreshToken.Hash(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || stored.IsRevoked)
            return false;

        if (ownerUserId is not null && stored.UserId != ownerUserId)
            return false;

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = reason;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task RevokeAllAsync(string userId, string reason, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    private async Task<(AuthTokens Tokens, RefreshToken Entity)> CreateAsync(ApplicationUser user, string? ipAddress)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtTokenService.GenerateAccessToken(user, roles);
        var rawRefreshToken = jwtTokenService.GenerateRefreshToken();

        var now = DateTime.UtcNow;
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = RefreshToken.Hash(rawRefreshToken),
            CreatedAt = now,
            CreatedByIp = ipAddress,
            ExpiresAt = now.AddDays(_settings.RefreshTokenDays)
        };

        var tokens = new AuthTokens(
            accessToken,
            now.AddMinutes(_settings.AccessTokenMinutes),
            rawRefreshToken,
            entity.ExpiresAt);

        return (tokens, entity);
    }
}