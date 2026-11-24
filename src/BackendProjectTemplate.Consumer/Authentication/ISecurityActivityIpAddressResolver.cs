namespace BackendProjectTemplate.Consumer.Authentication;

public interface ISecurityActivityIpAddressResolver
{
    Task<SecurityActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken);
}
