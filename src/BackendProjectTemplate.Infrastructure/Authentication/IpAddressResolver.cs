using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Authentication.Specifications;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class IpAddressResolver(IRepository<IpAddress> repository) : IIpAddressResolver
{
    public async Task<IpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        ipAddress = string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim();
        var address = await repository.FirstOrDefaultAsync(new IpAddressByValueSpecification(ipAddress), cancellationToken);
        if (address is null)
        {
            address = IpAddress.Create(ipAddress);
            await repository.AddAsync(address, cancellationToken);
        }

        return new IpAddressResolution(address.Id, address.GetCurrentLocation()?.Id);
    }
}
