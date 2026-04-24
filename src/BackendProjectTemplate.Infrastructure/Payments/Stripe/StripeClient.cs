using System.Net.Http.Json;

namespace BackendProjectTemplate.Infrastructure.Payments.Stripe;

internal sealed class StripeClient(IHttpClientFactory httpClientFactory) : IStripeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.Stripe);

    public Task<HttpResponseMessage> CreateCheckoutSessionAsync(
        object payload,
        CancellationToken cancellationToken) =>
        _httpClient.PostAsJsonAsync("/checkout/sessions", payload, cancellationToken);
}
