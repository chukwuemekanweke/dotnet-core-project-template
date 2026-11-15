using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Kyc.Kycaid;

internal sealed class KycaidOptionsValidator : IValidateOptions<KycaidOptions>
{
    public ValidateOptionsResult Validate(string? name, KycaidOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            return ValidateOptionsResult.Fail("Kyc:Kycaid:BaseUrl must be an absolute HTTPS URL.");
        if (string.IsNullOrWhiteSpace(options.ApiKey)) return ValidateOptionsResult.Fail("Kyc:Kycaid:ApiKey is required when KYCAID is enabled.");
        if (string.IsNullOrWhiteSpace(options.FormId)) return ValidateOptionsResult.Fail("Kyc:Kycaid:FormId is required when KYCAID is enabled.");
        if (options.RequestTimeoutSeconds is < 1 or > 120) return ValidateOptionsResult.Fail("Kyc:Kycaid:RequestTimeoutSeconds must be between 1 and 120.");
        if (options.TrustedFormHosts.Length == 0) return ValidateOptionsResult.Fail("At least one trusted KYCAID form host is required.");
        return ValidateOptionsResult.Success;
    }
}
