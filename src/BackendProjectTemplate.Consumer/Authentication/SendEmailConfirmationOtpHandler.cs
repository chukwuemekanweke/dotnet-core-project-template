using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class SendEmailConfirmationOtpHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    EmailConfirmationOtpSender emailConfirmationOtpSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SendEmailConfirmationOtpHandler> logger,
    IRepository<MessageInbox> messageInboxRepository) : BaseMessageHandler<SendEmailConfirmationOtpCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(
        SendEmailConfirmationOtpCommand message,
        CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException(
                "SendEmailConfirmationOtpCommand must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(
            message.StakeholderId.Value,
            cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to send an email confirmation OTP because stakeholder '{message.StakeholderId}' was not found.");
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to send an email confirmation OTP because user '{stakeholder.AppUserId}' was not found.");
        }

        if (user.EmailConfirmed)
        {
            AddOtpSendSkippedEvent(stakeholder.StakeholderId, ObservabilityFailureReasons.AlreadyConfirmed);
            return;
        }

        if (!await emailConfirmationOtpSender.SendAsync(stakeholder, cancellationToken))
        {
            AddOtpSendSkippedEvent(stakeholder.StakeholderId, ObservabilityFailureReasons.ActiveOtpExists);
            return;
        }

        CustomTelemetryContext.SetProperty(
            Observability.PropertyNames.Common.StakeholderId,
            stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSent,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(
        SendEmailConfirmationOtpCommand message)
    {
        yield break;
    }

    private void AddOtpSendSkippedEvent(Guid stakeholderId, string reason)
    {
        CustomTelemetryContext.SetProperty(
            Observability.PropertyNames.Common.StakeholderId,
            stakeholderId.ToString());
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, reason);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSendSkipped,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholderId, reason));
    }
}
