namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 (the algorithm used to sign and validate access tokens) requires at least a 256-bit key.
    public const int MinimumSigningKeyUtf8ByteCount = 32;

    public string Issuer { get; init; } = "BackendProjectTemplate";
    public string Audience { get; init; } = "BackendProjectTemplate.Clients";
    public string SigningKey { get; init; } = string.Empty;
    public int LifetimeMinutes { get; init; } = 5;
}
