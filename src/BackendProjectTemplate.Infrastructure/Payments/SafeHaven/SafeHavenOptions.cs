namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed class SafeHavenOptions
{
    public const string SectionName = "Payments:SafeHaven";

    public string BaseUrl { get; set; } = "https://placeholder.safehaven.local";
}
