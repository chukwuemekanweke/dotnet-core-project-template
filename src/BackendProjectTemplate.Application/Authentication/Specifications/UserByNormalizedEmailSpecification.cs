using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Application.Authentication.Specifications;

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
