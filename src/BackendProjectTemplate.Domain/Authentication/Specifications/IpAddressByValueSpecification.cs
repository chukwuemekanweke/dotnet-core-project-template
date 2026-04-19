using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Domain.Authentication.Specifications;

public sealed class IpAddressByValueSpecification : Specification<IpAddress>
{
    public IpAddressByValueSpecification(string value)
    {
        Where(ipAddress => ipAddress.Value == value);
        AddInclude(ipAddress => ipAddress.Locations);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
