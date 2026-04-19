using System.Text.Json;
using BackendProjectTemplate.Domain.Authentication.Services;
using Microsoft.Extensions.Options;
using Polly;

namespace BackendProjectTemplate.Infrastructure.Authentication;

internal sealed class IpInfoClient(IHttpClientFactory httpClientFactory, IOptions<IpInfoOptions> options) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.IpInfo);
    private readonly string _accessToken = options.Value.AccessToken;
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
        var payload = await JsonSerializer.DeserializeAsync<IpInfoResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return string.IsNullOrWhiteSpace(payload?.Country)
            ? null
            : new IpGeolocation(payload.City, payload.Region, payload.Country);
    }

    private string BuildRequestUri(string ipAddress) =>
        string.IsNullOrWhiteSpace(_accessToken)
            ? $"/{ipAddress}/json"
            : $"/{ipAddress}/json?token={_accessToken}";

    private sealed record IpInfoResponse(string? City, string? Region, string? Country);
}
