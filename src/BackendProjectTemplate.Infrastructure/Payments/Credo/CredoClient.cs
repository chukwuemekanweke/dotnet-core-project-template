using BackendProjectTemplate.Infrastructure.Payments.Credo.Payloads;
using Microsoft.Extensions.Options;
using Polly;

namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal sealed class CredoClient(
    IHttpClientFactory httpClientFactory,
    IOptions<CredoOptions> options) : ICredoClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.Credo);
    private readonly CredoOptions _options = options.Value;

    private readonly AsyncPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

    public Task<CredoInitializeTransactionResponse> InitializeTransactionAsync(
        CredoInitializeTransactionRequest request,
        CancellationToken cancellationToken) =>
        _retryPolicy.ExecuteAsync(
            async ct =>
            {
                ApplyAuthorizationHeader(_options.PublicKey);

                using var response = await _httpClient.PostAsJsonAsync(
                    "/transaction/initialize",
                    CreatePayload(request),
                    ct);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsAsync<CredoInitializeTransactionHttpResponse>(ct)
                    ?? throw new InvalidOperationException("Credo initialize transaction response was empty.");

                if (payload.Data is null)
                {
                    throw new InvalidOperationException("Credo initialize transaction response did not contain data.");
                }

                return new CredoInitializeTransactionResponse(
                    payload.Data.AuthorizationUrl,
                    payload.Data.Reference,
                    payload.Data.CredoReference,
                    payload.Data.Crn);
            },
            cancellationToken);

    public Task<CredoVerifyTransactionResponse> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken) =>
        _retryPolicy.ExecuteAsync(
            async ct =>
            {
                ApplyAuthorizationHeader(_options.SecretKey);

                using var response = await _httpClient.GetAsync(
                    $"/transaction/{Uri.EscapeDataString(reference)}/verify",
                    ct);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsAsync<CredoVerifyTransactionHttpResponse>(ct)
                    ?? throw new InvalidOperationException("Credo verify transaction response was empty.");

                if (payload.Data is null)
                {
                    throw new InvalidOperationException("Credo verify transaction response did not contain data.");
                }

                return new CredoVerifyTransactionResponse(
                    payload.Message,
                    payload.Data.BusinessCode,
                    payload.Data.TransRef,
                    payload.Data.BusinessRef,
                    payload.Data.DebitedAmount,
                    payload.Data.TransAmount,
                    payload.Data.TransFeeAmount,
                    payload.Data.SettlementAmount,
                    payload.Data.CustomerId,
                    payload.Data.TransactionDate,
                    payload.Data.ChannelId,
                    payload.Data.CurrencyCode,
                    payload.Data.Status,
                    payload.Data.Metadata?.Select(item => new CredoMetadataEntry(
                        item.InsightTag,
                        item.InsightTagValue,
                        item.InsightTagDisplay)).ToArray());
            },
            cancellationToken);

    private CredoInitializeTransactionPayload CreatePayload(CredoInitializeTransactionRequest request) =>
        new(
            request.Amount,
            request.Email,
            request.CustomerPhoneNumber,
            request.CustomerFirstName,
            request.CustomerLastName,
            request.Currency,
            request.Reference,
            _options.CallbackUrl,
            _options.Channels,
            _options.Bearer,
            new Dictionary<string, string>(),
            request.Narration,
            _options.InitializeAccount);

    private void ApplyAuthorizationHeader(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Credo API key is not configured.");
        }

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey.Trim());
    }
}
