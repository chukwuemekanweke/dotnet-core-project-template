using BackendProjectTemplate.Domain.Identity.Entities;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IAccessTokenService
{
    AccessToken Generate(AppUser user);
}
