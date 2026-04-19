namespace BackendProjectTemplate.Domain.Authentication.Services;

public interface IIpGeolocationService
{
    Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken);
}
