namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IGoogleIdentityTokenService
{
    Task<GoogleIdentityTokenPayload?> ValidateAsync(
        string idToken,
        string expectedNonce,
        CancellationToken cancellationToken);
}
