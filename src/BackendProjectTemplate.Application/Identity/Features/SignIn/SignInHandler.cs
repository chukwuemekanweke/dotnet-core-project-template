using BackendProjectTemplate.Application.Identity.Specifications;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Identity.Entities;

namespace BackendProjectTemplate.Application.Identity.Features.SignIn;

public sealed class SignInHandler(
    IRepository<AppUser> users,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService)
{
    public async Task<SignInResult> HandleAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = AppUser.NormalizeEmail(request.Email);
        var user = await users.FirstOrDefaultAsync(new UserByNormalizedEmailSpecification(normalizedEmail), cancellationToken);

        if (user is null)
        {
            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        if (!user.IsEmailVerified)
        {
            return new SignInResult(SignInStatus.EmailNotVerified, null);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        return new SignInResult(SignInStatus.Success, accessTokenService.Generate(user));
    }
}

public sealed record SignInResult(SignInStatus Status, AccessToken? AccessToken);

public enum SignInStatus
{
    Success = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3
}
