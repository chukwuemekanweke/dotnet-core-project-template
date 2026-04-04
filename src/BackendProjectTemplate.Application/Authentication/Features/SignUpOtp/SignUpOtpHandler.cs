using BackendProjectTemplate.Domain.Common.Authentication;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        if (user.EmailConfirmed)
        {
            return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
        }

        if (!await identityService.VerifySignUpOtpAsync(user, request.Otp))
        {
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        var now = timeProvider.GetUtcNow();
        user.MarkEmailVerified(now);

        var updateResult = await identityService.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Failed to update the user after OTP verification.");
        }

        return new SignUpOtpResult(SignUpOtpStatus.Success);
    }
}
