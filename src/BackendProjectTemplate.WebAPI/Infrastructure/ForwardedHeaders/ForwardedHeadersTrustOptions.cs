namespace BackendProjectTemplate.WebAPI.Infrastructure.ForwardedHeaders;

/// <summary>
/// Reverse proxies whose X-Forwarded-For / X-Forwarded-Proto headers are trusted. Loopback is always trusted
/// (framework default); containerized hosts add the address their proxy connects from, such as the Docker
/// network gateway behind a loopback-published port.
/// </summary>
public sealed class ForwardedHeadersTrustOptions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Exact proxy IP addresses, for example "172.30.231.1".</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Proxy networks in CIDR notation, for example "10.0.0.0/24".</summary>
    public string[] KnownNetworks { get; init; } = [];
}
