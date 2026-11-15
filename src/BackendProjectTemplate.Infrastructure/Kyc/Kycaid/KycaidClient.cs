using BackendProjectTemplate.Domain.Kyc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BackendProjectTemplate.Infrastructure.Kyc.Kycaid;

internal sealed class KycaidClient(HttpClient httpClient, IOptions<KycaidOptions> options) : IKycProviderService
{
    private readonly KycaidOptions _options = options.Value;

    public string ProviderKey => KycProviderKeys.Kycaid;

    public async Task<KycSessionResult> CreateSessionAsync(KycSessionRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/forms/{Uri.EscapeDataString(_options.FormId)}/urls")
        {
            Content = JsonContent.Create(new CreateFormUrlRequest(request.ApplicantReference, request.ExternalApplicantId, _options.RedirectUrl))
        };
        ApplyApiKey(message);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        EnsureSuccess(response);
        var payload = await response.Content.ReadFromJsonAsync<CreateFormUrlResponse>(cancellationToken)
            ?? throw new InvalidOperationException("KYCAID form URL response was empty.");
        if (!Uri.TryCreate(payload.FormUrl, UriKind.Absolute, out var formUrl) || formUrl.Scheme != Uri.UriSchemeHttps ||
            !_options.TrustedFormHosts.Contains(formUrl.Host, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("KYCAID returned an untrusted verification URL.");
        }

        return new(payload.VerificationId, payload.FormToken, formUrl, payload.VerificationAttemptsLeft);
    }

    public async Task<KycProviderVerificationResult> GetVerificationAsync(string verificationReference, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/verifications/{Uri.EscapeDataString(verificationReference)}");
        ApplyApiKey(message);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        EnsureSuccess(response);
        var payload = await response.Content.ReadFromJsonAsync<GetVerificationResponse>(cancellationToken)
            ?? throw new InvalidOperationException("KYCAID verification response was empty.");
        return new(payload.VerificationId, payload.ApplicantId, payload.Status, payload.Verified);
    }

    private void ApplyApiKey(HttpRequestMessage message) => message.Headers.TryAddWithoutValidation("API-Key", _options.ApiKey);

    private void EnsureEnabled()
    {
        if (!_options.Enabled) throw new KycProviderRequestRejectedException("KYCAID integration is disabled.");
    }

    // A 4xx answer means KYCAID did not perform the operation, so callers may safely retry it.
    // 408 is excluded: the request may still have been processed. Anything else stays an HttpRequestException
    // whose outcome is unknown.
    private static void EnsureSuccess(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        if (statusCode is >= 400 and < 500 && response.StatusCode != HttpStatusCode.RequestTimeout)
        {
            throw new KycProviderRequestRejectedException(
                $"KYCAID rejected the request with status {statusCode}.",
                new HttpRequestException(response.ReasonPhrase, null, response.StatusCode));
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed record CreateFormUrlRequest(
        [property: JsonPropertyName("applicant_id")] string? ApplicantId,
        [property: JsonPropertyName("external_applicant_id")] string ExternalApplicantId,
        [property: JsonPropertyName("redirect_url")] string? RedirectUrl);

    private sealed record CreateFormUrlResponse(
        [property: JsonPropertyName("verification_id")] string? VerificationId,
        [property: JsonPropertyName("form_token")] string FormToken,
        [property: JsonPropertyName("form_url")] string FormUrl,
        [property: JsonPropertyName("verification_attempts_left")] int? VerificationAttemptsLeft);

    private sealed record GetVerificationResponse(
        [property: JsonPropertyName("verification_id")] string VerificationId,
        [property: JsonPropertyName("applicant_id")] string? ApplicantId,
        string Status,
        bool? Verified);
}
