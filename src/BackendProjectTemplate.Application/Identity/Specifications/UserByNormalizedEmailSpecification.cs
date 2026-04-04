using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Identity.Entities;

namespace BackendProjectTemplate.Application.Identity.Specifications;

public sealed class UserByNormalizedEmailSpecification : Specification<AppUser>
{
    public UserByNormalizedEmailSpecification(string normalizedEmail, bool tracked = false)
    {
        Where(user => user.NormalizedEmail == normalizedEmail);

        if (tracked)
        {
            EnableTracking();
        }
    }
}
