using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Specifications;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class LoginActivityIpAddressResolver(
    IRepository<IpAddress> ipAddressRepository,
    IRepository<IpAddressLocation> ipAddressLocationRepository) : ILoginActivityIpAddressResolver
{
    public async Task<LoginActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var existingIpAddress = await ipAddressRepository.FirstOrDefaultAsync(
            new IpAddressByValueSpecification(ipAddress),
            cancellationToken);

        var persistedIpAddress = existingIpAddress;
        if (persistedIpAddress is null)
        {
            persistedIpAddress = IpAddress.Create(ipAddress);
            await ipAddressRepository.AddAsync(persistedIpAddress, cancellationToken);
        }

        var currentIpAddressLocation = await ipAddressLocationRepository.FirstOrDefaultAsync(
            new CurrentIpAddressLocationByIpAddressIdSpecification(persistedIpAddress.Id),
            cancellationToken);

        return new LoginActivityIpAddressResolution(persistedIpAddress.Id, currentIpAddressLocation?.Id);
    }
}
