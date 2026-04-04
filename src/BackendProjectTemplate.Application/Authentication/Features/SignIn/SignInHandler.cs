using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignIn;

public sealed class SignInHandler(
    IAuthenticationIdentityService identityService,
    IAccessTokenService accessTokenService)
{
    public async Task<SignInResult> HandleAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        if (!user.EmailConfirmed)
        {
            return new SignInResult(SignInStatus.EmailNotVerified, null);
        }

        if (!await identityService.CheckPasswordAsync(user, request.Password))
        {
            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        return new SignInResult(SignInStatus.Success, accessTokenService.Generate(user));
    }
}
