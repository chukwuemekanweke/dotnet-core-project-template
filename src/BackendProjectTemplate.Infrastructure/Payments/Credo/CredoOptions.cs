namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

public sealed class CredoOptions
{
    public const string SectionName = "Payments:Credo";

    public string BaseUrl { get; set; } = "https://placeholder.credo.local";
}
