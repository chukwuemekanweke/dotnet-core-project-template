using BackendProjectTemplate.Infrastructure.Authentication;
using System.Text;

namespace BackendProjectTemplate.Infrastructure.Configuration;

public sealed class ProductionSecretRequirement
{
    private static readonly string[] KnownJwtPlaceholders =
    [
        "super-secret-template-signing-key-change-me",
        "development-only-signing-key-not-for-production",
        "change-me",
        "changeme"
    ];

    private ProductionSecretRequirement(
        string configurationKey,
        int minimumUtf8ByteCount,
        IReadOnlyCollection<string> disallowedValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);
        ConfigurationKey = configurationKey;
        MinimumUtf8ByteCount = minimumUtf8ByteCount;
        DisallowedValues = disallowedValues;
    }

    public string ConfigurationKey { get; }
    public int MinimumUtf8ByteCount { get; }
    public IReadOnlyCollection<string> DisallowedValues { get; }

    public string FileName => ConfigurationKey
        .Replace(":", "__", StringComparison.Ordinal)
        .ToUpperInvariant();

    public static ProductionSecretRequirement Required(string configurationKey) =>
        new(configurationKey, 1, []);

    public static ProductionSecretRequirement JwtSigningKey() =>
        new("Jwt:SigningKey", JwtOptions.MinimumSigningKeyUtf8ByteCount, KnownJwtPlaceholders);

    internal string? GetValidationError(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "the delivered secret is blank or contains only whitespace";
        }

        if (Encoding.UTF8.GetByteCount(value) < MinimumUtf8ByteCount)
        {
            return $"the delivered secret must contain at least {MinimumUtf8ByteCount} UTF-8 bytes";
        }

        if (DisallowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return "the delivered secret is a known placeholder value";
        }

        return null;
    }
}
