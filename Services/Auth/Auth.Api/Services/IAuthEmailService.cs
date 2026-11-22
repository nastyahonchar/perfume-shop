using Auth.Api.Data;

namespace Auth.Api.Services;

public interface IAuthEmailService
{
    Task SendEmailConfirmationAsync(ApplicationUser user, string token, CancellationToken ct = default);

    Task SendPasswordResetAsync(ApplicationUser user, string token, CancellationToken ct = default);
}

public sealed class ConsoleEmailService(ILogger<ConsoleEmailService> logger) : IAuthEmailService
{
    public Task SendEmailConfirmationAsync(ApplicationUser user, string token, CancellationToken ct = default)
    {
        logger.LogInformation(
            "MOCK EMAIL to {Email}: confirm your email. UserId {UserId}, token {Token}",
            user.Email, user.Id, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(ApplicationUser user, string token, CancellationToken ct = default)
    {
        logger.LogInformation(
            "MOCK EMAIL to {Email}: reset your password. UserId {UserId}, token {Token}",
            user.Email, user.Id, token);
        return Task.CompletedTask;
    }
}