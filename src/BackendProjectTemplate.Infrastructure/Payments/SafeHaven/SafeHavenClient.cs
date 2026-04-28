using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    public Task<SafeHavenTokenResponse> ExchangeTokenAsync(
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

    public async Task<SafeHavenResponse<SafeHavenVirtualAccount>> CreateVirtualAccountAsync(
        SafeHavenCreateVirtualAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new
        {
            accountName = request.AccountName,
            validFor = _options.ValidFor,
            settlementAccount = new
            {
                bankCode = _options.SettlementBankCode,
                accountNumber = _options.SettlementAccountNumber
            },
            amountControl = "fixed",
            amount = request.Amount,
            callbackUrl = _options.CallbackUrl,
            externalReference = request.ExternalReference
        };

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/virtual-accounts");
            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
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

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/identity/v2");
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
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
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/identity/v2/validate");
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
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

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/accounts/v2/subaccount");
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var wrapper = await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenSubAccount>>(SerializerOptions, ct)
                ?? throw new InvalidOperationException("SafeHaven create sub-account response was empty.");

            return wrapper;
        }, cancellationToken);
    }

    public async Task<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>> GetAccountStatementAsync(
        string accountId,
        SafeHavenAccountStatementRequest? request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var queryParams = new List<string>();
        if (request is not null)
        {
            if (request.Page > 0)
            {
                queryParams.Add($"page={request.Page}");
            }
            if (request.Limit > 0)
            {
                queryParams.Add($"limit={request.Limit}");
            }
            if (!string.IsNullOrWhiteSpace(request.FromDate))
            {
                queryParams.Add($"fromDate={Uri.EscapeDataString(request.FromDate)}");
            }
            if (!string.IsNullOrWhiteSpace(request.ToDate))
            {
                queryParams.Add($"toDate={Uri.EscapeDataString(request.ToDate)}");
            }
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                queryParams.Add($"type={Uri.EscapeDataString(request.Type)}");
            }
        }

        var uri = $"/accounts/{accountId}/statement";
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
}
