namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class IpInfoOptions
{
    public const string SectionName = "IpGeolocation:IpInfo";

    public string AccessToken { get; set; } = string.Empty;
}
