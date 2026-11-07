namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed class PasskeyAuthenticationOptions
{
    public const string SectionName = "Authentication:Passkeys";

    public string RelyingPartyId { get; set; } = "localhost";
    public string RelyingPartyName { get; set; } = "BackendProjectTemplate";
    public string[] AllowedOrigins { get; set; } = ["http://localhost"];
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan AuthenticatorTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RelyingPartyId) || Uri.CheckHostName(RelyingPartyId) == UriHostNameType.Unknown)
            throw new InvalidOperationException("A valid passkey relying-party ID is required.");
        if (string.IsNullOrWhiteSpace(RelyingPartyName))
            throw new InvalidOperationException("A passkey relying-party name is required.");
        if (AllowedOrigins.Length == 0)
            throw new InvalidOperationException("At least one passkey origin is required.");
        foreach (var origin in AllowedOrigins)
        {
            if (origin.Contains('*', StringComparison.Ordinal)
                || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                    && !uri.IsLoopback))
            {
                throw new InvalidOperationException($"Passkey origin '{origin}' is invalid. Origins must be exact HTTPS origins or loopback development origins.");
            }

            if (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException($"Passkey origin '{origin}' must not contain a path, query, or fragment.");
        }
        if (ChallengeLifetime <= TimeSpan.Zero || AuthenticatorTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("Passkey timeouts must be positive.");
    }
}
