using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}