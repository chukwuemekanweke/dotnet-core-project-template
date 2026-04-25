using System.Net.Http.Json;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed class SafeHavenClient(IHttpClientFactory httpClientFactory) : ISafeHavenClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.SafeHaven);

    public Task<HttpResponseMessage> CreateVirtualAccountPaymentAsync(
        object payload,
        CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("/payments/virtual-accounts", payload, cancellationToken);
}
