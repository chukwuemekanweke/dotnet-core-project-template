using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserCreatedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IOtpDeliveryService otpDeliveryService,
    ILogger<UserCreatedHandler> logger) : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor)
{
    protected override async Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(message.UserId);
        if (user is null)
        {
            logger.LogWarning(
                "Unable to send sign-up OTP for user {UserId} because the account could not be found.",
                message.UserId);

            return;
        }

        if (user.EmailConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP delivery for user {UserId} because the email is already confirmed.",
                message.UserId);

            return;
        }

        var otpCode = await identityService.GenerateSignUpOtpAsync(user);
        await otpDeliveryService.SendSignUpOtpAsync(user, otpCode, cancellationToken);
        var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(message.UserId, cancellationToken);
        var properties = new Dictionary<string, string>();
        if (stakeholder is not null)
        {
            CustomTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholder.StakeholderId.ToString());
            properties[Observability.StakeholderIdPropertyName] = stakeholder.StakeholderId.ToString();
        }

        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserCreatedProcessed, properties);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserCreated message)
    {
        yield break;
    }
}
