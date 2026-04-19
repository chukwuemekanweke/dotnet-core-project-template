using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Domain.Authentication.Specifications;

public sealed class RefreshTokenByHashSpecification : Specification<AuthenticationRefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Where(refreshToken => refreshToken.TokenHash == tokenHash);
        ApplyPaging(0, 1);
    }
}
