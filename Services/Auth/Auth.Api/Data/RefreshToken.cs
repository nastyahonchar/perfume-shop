using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Data;

// Refresh-токени зберігаються в таблиці RefreshTokens з датами створення (CreatedAt), закінчення (ExpiresAt, 7 днів)
// і відкликання (RevokedAt). У БД лежить лише SHA-256 хеш токена: витік бази не дає змоги його використати.
// При оновленні старий токен відкликається і посилається на новий (ReplacedByTokenHash) — це ротація.
public class RefreshToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}