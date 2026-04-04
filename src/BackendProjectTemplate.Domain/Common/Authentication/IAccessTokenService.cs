using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public interface IAccessTokenService
{
    AccessToken Generate(AppUser user);
}
