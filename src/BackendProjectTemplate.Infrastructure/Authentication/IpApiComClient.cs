using System.Text.Json;
using BackendProjectTemplate.Domain.Authentication.Services;
using Polly;

namespace BackendProjectTemplate.Infrastructure.Authentication;

internal sealed class IpApiComClient(IHttpClientFactory httpClientFactory) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.IpApiCom);
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
        using var response = await _httpClient.GetAsync($"/json/{ipAddress}?fields=status,country,regionName,city", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpApiComResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return payload is { Status: "success" }
            ? new IpGeolocation(payload.City, payload.RegionName, payload.Country)
            : null;
    }

    private sealed record IpApiComResponse(string Status, string? Country, string? RegionName, string? City);
}
