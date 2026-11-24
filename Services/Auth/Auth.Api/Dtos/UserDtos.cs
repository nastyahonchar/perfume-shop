using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public sealed record UserDto(
    string Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    DateTime CreatedAt,
    DateTimeOffset? LockoutEnd,
    IReadOnlyList<string> Roles);

public sealed record PagedUsersResponse(IReadOnlyList<UserDto> Items, int Total, int Page, int PageSize);

public sealed record RefreshTokenDto(
    int Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? RevokedReason,
    string? CreatedByIp,
    bool IsActive);

public class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 3)]
    public string? UserName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public string Role { get; set; } = "";
}