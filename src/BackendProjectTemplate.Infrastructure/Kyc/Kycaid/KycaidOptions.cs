namespace BackendProjectTemplate.Infrastructure.Kyc.Kycaid;

public sealed class KycaidOptions
{
    public const string SectionName = "Kyc:Kycaid";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://api.kycaid.com";
    public string ApiKey { get; init; } = string.Empty;
    public string FormId { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public int RequestTimeoutSeconds { get; init; } = 15;
    public string[] TrustedFormHosts { get; init; } = ["forms.kycaid.com"];
}
