using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication;

public sealed class TwoFactorProofVerifier(IAuthenticationIdentityService identityService)
{
    public async Task<bool> VerifyAsync(
        AppUser user,
        TwoFactorVerificationMethod method,
        string code)
    {
        return method switch
        {
            TwoFactorVerificationMethod.Authenticator =>
                await identityService.VerifyAuthenticatorTokenAsync(user, code),
            TwoFactorVerificationMethod.RecoveryCode =>
                (await identityService.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded,
            _ => false
        };
    }
}
