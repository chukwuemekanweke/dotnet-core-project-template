using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICurrentActor currentActor,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationStarted,
            ObservabilityEventProperties.Create(currentActor));

        var user = await identityService.FindByEmailAsync(request.Email);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(currentActor, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        if (user.EmailConfirmed)
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.AlreadyConfirmed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(currentActor, failureReason: ObservabilityFailureReasons.AlreadyConfirmed));
            return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
        }

        if (!await identityService.VerifySignUpOtpAsync(user, request.Otp))
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(currentActor, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        var now = timeProvider.GetUtcNow();
        user.MarkEmailVerified(now);
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var updateResult = await identityService.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Failed to update the user after OTP verification.");
        }

        await eventPublisher.PublishAsync(new UserEmailConfirmed
        {
            StakeholderId = stakeholder.Id,
            FlowId = currentActor.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationCompleted,
            ObservabilityEventProperties.Create(currentActor, stakeholder.Id));

        return new SignUpOtpResult(SignUpOtpStatus.Success);
    }
}
