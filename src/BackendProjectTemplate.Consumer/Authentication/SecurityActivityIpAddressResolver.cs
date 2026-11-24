using BackendProjectTemplate.Domain.Authentication.Services;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class SecurityActivityIpAddressResolver(IIpAddressResolver ipAddressResolver) : ISecurityActivityIpAddressResolver
{
    public async Task<SecurityActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var resolved = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        return new SecurityActivityIpAddressResolution(resolved.IpAddressId, resolved.IpAddressLocationId);
    }
}
