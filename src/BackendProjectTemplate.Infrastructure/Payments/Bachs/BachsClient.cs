using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal sealed class BachsClient(
    IHttpClientFactory httpClientFactory,
    IOptions<BachsOptions> options) : IBachsClient
{
    private readonly HttpClient _writeHttpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.Bachs);
    private readonly HttpClient _readHttpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.BachsRead);
    private readonly BachsOptions _options = options.Value;

    public Task<BachsCheckoutSessionResponse> CreateCheckoutSessionAsync(
        BachsCreateCheckoutSessionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        SendAsync(_writeHttpClient, HttpMethod.Post, "/v1/checkout-sessions", request, idempotencyKey, cancellationToken);

    public Task<BachsCheckoutSessionResponse> GetCheckoutSessionAsync(
        string checkoutId,
        CancellationToken cancellationToken) =>
        SendAsync(
            _readHttpClient,
            HttpMethod.Get,
            $"/v1/checkout-sessions/{Uri.EscapeDataString(checkoutId)}",
            null,
            null,
            cancellationToken);

    public Task<BachsCustomerResponse> CreateCustomerAsync(
        BachsCreateCustomerRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        SendAsync<BachsCreateCustomerRequest, BachsCustomerResponse>(
            _writeHttpClient,
            HttpMethod.Post,
            "/v1/customers",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<BachsChargeResponse> CreateChargeAsync(
        BachsCreateChargeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        SendAsync<BachsCreateChargeRequest, BachsChargeResponse>(
            _writeHttpClient,
            HttpMethod.Post,
            "/v1/charges",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<BachsChargeResponse> GetPaymentAsync(string paymentId, CancellationToken cancellationToken) =>
        SendAsync<object, BachsChargeResponse>(
            _readHttpClient,
            HttpMethod.Get,
            $"/v1/payments/{Uri.EscapeDataString(paymentId)}",
            null,
            null,
            cancellationToken);

    private Task<BachsCheckoutSessionResponse> SendAsync(
        HttpClient httpClient,
        HttpMethod method,
        string requestUri,
        BachsCreateCheckoutSessionRequest? payload,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        SendAsync<BachsCreateCheckoutSessionRequest, BachsCheckoutSessionResponse>(
            httpClient,
            method,
            requestUri,
            payload,
            idempotencyKey,
            cancellationToken);

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpClient httpClient,
        HttpMethod method,
        string requestUri,
        TRequest? payload,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? throw new InvalidOperationException("Bachs API key is not configured.")
            : _options.ApiKey.Trim();

        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Bachs response was empty.");
    }
}
