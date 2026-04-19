namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class IpApiComOptions
{
    public const string SectionName = "IpGeolocation:IpApiCom";

    public string ApiBaseUrl { get; set; } = "http://ip-api.com";
}
