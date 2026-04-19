namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class IpWhoIsOptions
{
    public const string SectionName = "IpGeolocation:IpWhoIs";

    public string ApiKey { get; set; } = string.Empty;
}
