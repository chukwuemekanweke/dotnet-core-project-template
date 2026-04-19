using BackendProjectTemplate.Domain.Authentication.Services;

namespace BackendProjectTemplate.Infrastructure.Authentication;

internal interface IIpGeolocationProvider
{
    Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken);
}
