namespace BackendProjectTemplate.WebAPI.Infrastructure.CrossOrigin;

/// <summary>
/// Browser origins allowed to call the API in Production, for example "https://template-user.chidelu.com".
/// Non-production environments allow any origin.
/// </summary>
public sealed class CrossOriginOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
