using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Authentication;

public sealed class UserSignInFailedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    ILogger<UserSignInFailedHandler> logger) : BaseMessageHandler<UserSignInFailed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserSignInFailed message, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(message.EmailAddress);
        if (user is null)
        {
            logger.LogInformation(
                "Skipping sign-in failure processing because no user could be resolved for email {EmailAddress}.",
                message.EmailAddress);

            return;
        }

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials)
        {
            var accessFailedResult = await identityService.AccessFailedAsync(user);
            if (!accessFailedResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to increment access-failed count for user {user.Id}.");
            }
        }

        StakeholderReadModel? stakeholder = null;
        if (message.StakeholderId.HasValue)
        {
            stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        }
        if (stakeholder is not null)
        {
            CustomTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholder.StakeholderId.ToString());
        }

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials &&
            await identityService.IsLockedOutAsync(user))
        {
            if (stakeholder is null)
            {
                throw new CannotProcessMessageNonTransientException(
                    $"Unable to process UserSignInFailed because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
            }

            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user)
                ?? throw new InvalidOperationException($"User {user.Id} is locked out but has no lockout end time.");

            await commandSender.SendAsync(
                new SendNotificationCommand(
                    stakeholder.TenantId,
                    stakeholder.CountryId,
                    NotificationType.AccountLocked,
                    NotificationMedium.Email,
                    new EmailNotificationContent(
                        user.Email ?? message.EmailAddress,
                        new Dictionary<string, string>
                        {
                            ["LockedUntilUtc"] = lockedUntilUtc.ToString("O")
                        }))
                {
                    StakeholderId = stakeholder.StakeholderId
                },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SignInFailureProcessed,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                stakeholder?.StakeholderId,
                message.FailureReason));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInFailed message)
    {
        yield break;
    }
}
