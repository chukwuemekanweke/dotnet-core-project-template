using System.Net.Http.Json;

namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal sealed class CredoClient(IHttpClientFactory httpClientFactory) : ICredoClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.Credo);

    public Task<HttpResponseMessage> CreatePaymentLinkAsync(
        object payload,
        CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("/payments/links", payload, cancellationToken);
}
