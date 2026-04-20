using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

public sealed class CompletePasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<CompletePasswordResetResult> HandleAsync(
        CompletePasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return new CompletePasswordResetResult(CompletePasswordResetStatus.UserNotFound);
        }

        var otpIsValid = await twoFactorOtpService.ValidateOtpAsync(
            user.Id,
            request.Otp,
            OtpIntent.PasswordReset,
            cancellationToken);
        if (!otpIsValid)
        {
            return new CompletePasswordResetResult(CompletePasswordResetStatus.InvalidOtp);
        }

        var resetResult = await identityService.ResetPasswordAsync(user, request.Password);
        if (!resetResult.Succeeded)
        {
            return new CompletePasswordResetResult(
                CompletePasswordResetStatus.ValidationFailed,
                resetResult.ToValidationDictionary());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var stakeholderId = await stakeholderResolver.GetRequiredIdAsync(user.Id, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetCompleted,
            new Dictionary<string, string>
            {
                [Observability.StakeholderIdPropertyName] = stakeholderId.ToString()
            });

        return new CompletePasswordResetResult(CompletePasswordResetStatus.Success);
    }
}
