using System.Text.Json;
using BackendProjectTemplate.Domain.Authentication.Services;
using Microsoft.Extensions.Options;
using Polly;

namespace BackendProjectTemplate.Infrastructure.Authentication;

internal sealed class IpWhoIsClient(IHttpClientFactory httpClientFactory, IOptions<IpWhoIsOptions> options) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("IpWhoIsClient");
    private readonly string _apiKey = options.Value.ApiKey;
    private readonly AsyncPolicy<IpGeolocation?> _retryPolicy = Policy<IpGeolocation?>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

    public Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken) =>
        _retryPolicy.ExecuteAsync(
            ct => GetGeolocationCoreAsync(ipAddress, ct),
            cancellationToken);

    private async Task<IpGeolocation?> GetGeolocationCoreAsync(string ipAddress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(BuildRequestUri(ipAddress), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpWhoIsResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return payload is { Success: true }
            ? new IpGeolocation(payload.City, payload.Region, payload.Country)
            : null;
    }

    private string BuildRequestUri(string ipAddress) =>
        string.IsNullOrWhiteSpace(_apiKey)
            ? $"https://ipwho.is/{ipAddress}"
            : $"https://ipwho.is/{ipAddress}?key={_apiKey}";

    private sealed record IpWhoIsResponse(bool Success, string? City, string? Region, string? Country);
}
