using BackendProjectTemplate.Application.Authentication.AppUserStakeholders;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    AppUserStakeholderResolver appUserStakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpCommand request, CancellationToken cancellationToken)
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
        var appUserStakeholder = await appUserStakeholderResolver.GetRequiredStakeholderAsync(user.Id, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var updateResult = await identityService.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Failed to update the user after OTP verification.");
        }

        await eventPublisher.PublishAsync(new UserEmailConfirmed
        {
            StakeholderId = appUserStakeholder.StakeholderId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var properties = new Dictionary<string, string>
        {
            [Observability.StakeholderIdPropertyName] = appUserStakeholder.StakeholderId.ToString()
        };

        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.OtpConfirmed, properties);

        return new SignUpOtpResult(SignUpOtpStatus.Success);
    }
}
