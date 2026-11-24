using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSecurityActivityOccurredHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ISecurityActivityIpAddressResolver securityActivityIpAddressResolver,
    IRepository<SecurityActivity> securityActivityRepository,
    IUnitOfWork unitOfWork,
    IUserAgentParserService userAgentParserService,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<UserSecurityActivityOccurredHandler> logger) : BaseMessageHandler<UserSecurityActivityOccurred>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserSecurityActivityOccurred message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserSecurityActivityOccurred must contain a valid stakeholder actor id.");
        }

        var activityType = MapActivityType(message.ActivityType);

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSecurityActivityOccurred because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var userAgentInfo = userAgentParserService.Parse(message.UserAgent);
        var ipAddressResolution = await securityActivityIpAddressResolver.ResolveAsync(message.IpAddress, cancellationToken);

        var securityActivity = SecurityActivity.CreateAccountSecurityChange(
            activityType,
            stakeholder.StakeholderId,
            stakeholder.TenantId,
            ipAddressResolution.IpAddressId,
            ipAddressResolution.IpAddressLocationId,
            message.UserAgent,
            userAgentInfo.DeviceName,
            userAgentInfo.DevicePlatform,
            userAgentInfo.BrowserName,
            message.OccuredAt);

        await securityActivityRepository.AddAsync(securityActivity, cancellationToken);
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SecurityActivityPostProcessingCompleted,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSecurityActivityOccurred message)
    {
        yield break;
    }

    private static SecurityActivityType MapActivityType(string activityType) => activityType switch
    {
        UserSecurityActivityTypes.PasskeyAdded => SecurityActivityType.PasskeyAdded,
        UserSecurityActivityTypes.PasskeyRemoved => SecurityActivityType.PasskeyRemoved,
        UserSecurityActivityTypes.TwoFactorEnabled => SecurityActivityType.TwoFactorEnabled,
        UserSecurityActivityTypes.TwoFactorDisabled => SecurityActivityType.TwoFactorDisabled,
        UserSecurityActivityTypes.TwoFactorRecoveryCodesRegenerated => SecurityActivityType.TwoFactorRecoveryCodesRegenerated,
        UserSecurityActivityTypes.PasswordChanged => SecurityActivityType.PasswordChanged,
        _ => throw new CannotProcessMessageNonTransientException(
            $"Unable to process UserSecurityActivityOccurred because activity type '{activityType}' is not supported.")
    };
}
