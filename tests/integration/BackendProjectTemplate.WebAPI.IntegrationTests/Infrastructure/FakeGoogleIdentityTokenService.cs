using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class FakeGoogleIdentityTokenService : IGoogleIdentityTokenService
{
    private readonly Dictionary<string, GoogleIdentityTokenPayload> _tokens = new(StringComparer.Ordinal);

    public void Register(string idToken, GoogleIdentityTokenPayload payload) =>
        _tokens[idToken] = payload;

    public Task<GoogleIdentityTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        _tokens.TryGetValue(idToken, out var payload);
        return Task.FromResult(payload);
    }
}
