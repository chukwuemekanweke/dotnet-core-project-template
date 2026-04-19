using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Domain.Authentication.Specifications;

public sealed class CurrentIpAddressLocationByIpAddressIdSpecification : Specification<IpAddressLocation>
{
    public CurrentIpAddressLocationByIpAddressIdSpecification(Guid ipAddressId)
    {
        Where(ipAddressLocation =>
            ipAddressLocation.IpAddressId == ipAddressId &&
            ipAddressLocation.IsCurrentLocation);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
