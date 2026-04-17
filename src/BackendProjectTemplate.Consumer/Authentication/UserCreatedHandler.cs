using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserCreatedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IOtpDeliveryService otpDeliveryService,
    ILogger<UserCreatedHandler> logger) : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserCreated must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no user could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (user.EmailConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP delivery for email {EmailAddress} because the email is already confirmed.",
                message.EmailAddress);

            return;
        }

        var otpCode = await identityService.GenerateSignUpOtpAsync(user);
        await otpDeliveryService.SendSignUpOtpAsync(user, otpCode, cancellationToken);
        var properties = new Dictionary<string, string>();
        if (message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, message.StakeholderId.Value.ToString());
            properties[Observability.StakeholderIdPropertyName] = message.StakeholderId.Value.ToString();
        }

        CustomTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserCreatedProcessed, properties);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserCreated message)
    {
        yield break;
    }
}
