using System.Text.Json.Serialization;

namespace Platform.Aggregator.Auth;

// Aggregator звертається до IdentityServer (POST /connect/token, grant_type=client_credentials) зі своїм
// client_id і client_secret. У токені немає sub: це ідентичність сервісу, а не користувача.
// Токен кешується до завершення терміну дії, а якщо IdentityServer недоступний, запит іде без токена.
public sealed class ClientCredentialsTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ClientCredentialsTokenProvider> logger)
{
    public const string HttpClientName = "identityserver-token";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _token;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _token;
            }

            var section = configuration.GetSection("IdentityServer");
            var authority = section["Authority"];
            if (string.IsNullOrEmpty(authority))
            {
                return null;
            }

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = section["ClientId"] ?? string.Empty,
                ["client_secret"] = section["ClientSecret"] ?? string.Empty,
                ["scope"] = section["Scope"] ?? string.Empty
            };

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync(
                $"{authority.TrimEnd('/')}/connect/token", new FormUrlEncodedContent(form), ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                ?? throw new InvalidOperationException("Empty token response");

            _token = payload.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn - 60);

            logger.LogInformation(
                "Obtained Client Credentials token for {ClientId}, expires in {Seconds}s",
                form["client_id"], payload.ExpiresIn);

            return _token;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not obtain Client Credentials token, calls continue without it");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}