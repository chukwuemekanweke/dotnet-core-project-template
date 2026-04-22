using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

public sealed class CompletePasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    StakeholderResolver stakeholderResolver,
    ICurrentActor currentActor,
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
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.UserNotFound);
            return new CompletePasswordResetResult(CompletePasswordResetStatus.UserNotFound);
        }

        var otpIsValid = await twoFactorOtpService.ValidateOtpAsync(
            user.Id,
            request.Otp,
            OtpIntent.PasswordReset,
            cancellationToken);
        if (!otpIsValid)
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.InvalidOtp);
            return new CompletePasswordResetResult(CompletePasswordResetStatus.InvalidOtp);
        }

        var resetResult = await identityService.ResetPasswordAsync(user, request.Password);
        if (!resetResult.Succeeded)
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.ValidationFailed);
            return new CompletePasswordResetResult(
                CompletePasswordResetStatus.ValidationFailed,
                resetResult.ToValidationDictionary());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var stakeholderId = await stakeholderResolver.GetRequiredIdAsync(user.Id, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetCompleted,
            ObservabilityEventProperties.Create(currentActor, stakeholderId));

        return new CompletePasswordResetResult(CompletePasswordResetStatus.Success);
    }
}
