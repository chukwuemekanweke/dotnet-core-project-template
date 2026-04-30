using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BackendProjectTemplate.Infrastructure.Payments.SafeHaven.Payloads;
using Microsoft.Extensions.Options;
using Polly;

namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed class SafeHavenClient(
    IHttpClientFactory httpClientFactory,
    IOptions<SafeHavenOptions> options,
    TimeProvider timeProvider) : ISafeHavenClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.SafeHaven);
    private readonly SafeHavenOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    private SafeHavenTokenResponse? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    private readonly AsyncPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

    public async Task<SafeHavenResponse<SafeHavenVirtualAccount>> CreateVirtualAccountAsync(
        SafeHavenCreateVirtualAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenCreateVirtualAccountPayload(
            AccountName: request.AccountName,
            ValidFor: _options.ValidFor,
            SettlementAccount: new SafeHavenSettlementAccountPayload(
                BankCode: _options.SettlementBankCode,
                AccountNumber: _options.SettlementAccountNumber),
            AmountControl: "fixed",
            Amount: request.Amount,
            CallbackUrl: _options.CallbackUrl,
            ExternalReference: request.ExternalReference);

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var response = await PostAsJsonAsync("/virtual-accounts", payload, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenVirtualAccount>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven create virtual account response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<SafeHavenVirtualAccount>?> GetVirtualAccountAsync(
        string virtualAccountId,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/virtual-accounts/{virtualAccountId}");
            using var response = await _httpClient.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var wrapper = await JsonSerializer.DeserializeAsync<SafeHavenResponse<SafeHavenVirtualAccount>>(stream, SerializerOptions, ct);
            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<SafeHavenIdentityVerification>> InitiateIdentityVerificationAsync(
        SafeHavenInitiateVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenInitiateVerificationPayload(
            Type: request.Type,
            Number: request.Number,
            DebitAccountNumber: request.DebitAccountNumber,
            Async: false);

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var response = await PostAsJsonAsync("/identity/v2", payload, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenIdentityVerification>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven identity verification initiation response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<SafeHavenIdentityVerification>> ValidateIdentityVerificationAsync(
        SafeHavenValidateVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var response = await PostAsJsonAsync("/identity/v2/validate", request, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenIdentityVerification>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven identity verification validation response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<SafeHavenSubAccount>> CreateSubAccountAsync(
        SafeHavenCreateSubAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenCreateSubAccountPayload(
            PhoneNumber: request.PhoneNumber,
            Email: request.Email,
            ExternalReference: request.ExternalReference,
            IdentityType: request.IdentityType,
            IdentityNumber: request.IdentityNumber,
            IdentityId: request.IdentityId,
            Otp: request.Otp,
            CallbackUrl: _options.CallbackUrl,
            AutoSweep: true,
            AutoSweepDetails: new SafeHavenCreateSubAccountAutoSweepDetailsPayload(
                AccountNumber: _options.AutoSweepAccountNumber,
                Schedule: "Instant"));

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var response = await PostAsJsonAsync("/accounts/v2/subaccount", payload, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenSubAccount>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven create sub-account response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>> GetAccountStatementAsync(
        SafeHavenAccountStatementRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var queryParams = new List<string>();
        if (request.Page > 0)
        {
            queryParams.Add($"page={request.Page}");
        }
        if (request.Limit > 0)
        {
            queryParams.Add($"limit={request.Limit}");
        }
        if (request.FromDate.HasValue)
        {
            queryParams.Add($"fromDate={Uri.EscapeDataString(request.FromDate.Value.ToShortDateString())}");
        }
        if (request.ToDate.HasValue)
        {
            queryParams.Add($"toDate={Uri.EscapeDataString(request.ToDate.Value.ToShortDateString())}");
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            queryParams.Add($"type={Uri.EscapeDataString(request.Type)}");
        }

        var uri = $"/accounts/{request.AccountId}/statement";
        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, uri);
            using var response = await _httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven account statement response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && _tokenExpiry > _timeProvider.GetUtcNow().AddMinutes(1))
        {
            return;
        }

        var request = new SafeHavenTokenRequest(
            "client_credentials",
            _options.ClientId,
            _options.ClientAssertion,
            "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");

        var token = await ExchangeTokenAsync(request, cancellationToken);

        _cachedToken = token;
        _tokenExpiry = _timeProvider.GetUtcNow().AddSeconds(token.ExpiresIn - 60);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken!.AccessToken);
        request.Headers.TryAddWithoutValidation("ClientID", _cachedToken.IbsClientId);
        return request;
    }

    private Task<HttpResponseMessage> PostAsJsonAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ApplyAuthenticationHeaders();
        return _httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
    }

    private void ApplyAuthenticationHeaders()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken!.AccessToken);
        _httpClient.DefaultRequestHeaders.Remove("ClientID");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("ClientID", _cachedToken.IbsClientId);
    }

    private Task<SafeHavenTokenResponse> ExchangeTokenAsync(
        SafeHavenTokenRequest request,
        CancellationToken cancellationToken) =>
        _retryPolicy.ExecuteAsync(async ct =>
        {
            using var response = await _httpClient.PostAsJsonAsync("/oauth2/token", request, ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<SafeHavenTokenResponse>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven token response was empty.");

            return token;
        }, cancellationToken);

}
