using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.AspNetCore.Identity;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class SendSubscriptionRenewalReminderHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionRenewal> renewalRepository,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<BillingAccount> billingAccountRepository,
    IRepository<Stakeholder> stakeholderRepository,
    UserManager<AppUser> userManager,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<SendSubscriptionRenewalReminderHandler> logger) : BaseMessageHandler<SendSubscriptionRenewalReminderCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(
        SendSubscriptionRenewalReminderCommand message,
        CancellationToken cancellationToken)
    {
        var renewal = await renewalRepository.FirstOrDefaultAsync(
            new SubscriptionRenewalByIdSpecification(message.RenewalId),
            cancellationToken);
        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(message.SubscriptionId),
            cancellationToken);
        if (renewal is null ||
            subscription is null ||
            renewal.TenantId != message.TenantId ||
            renewal.SubscriptionId != subscription.Id ||
            renewal.Status is SubscriptionRenewalStatus.Fulfilled or SubscriptionRenewalStatus.Cancelled ||
            subscription.Status != SubscriptionStatus.Active ||
            subscription.CurrentPeriodEndUtc != renewal.TargetPeriodStartUtc)
        {
            return;
        }

        if (!renewal.MarkReminderSent(message.ReminderSequence))
        {
            return;
        }

        var billingAccount = await billingAccountRepository.GetByIdAsync(renewal.BillingAccountId, cancellationToken);
        if (billingAccount?.StakeholderId is not Guid stakeholderId)
        {
            logger.LogWarning("Skipped renewal reminder {RenewalId} because its billing account has no stakeholder recipient.", renewal.Id);
            return;
        }

        var stakeholder = await stakeholderRepository.FirstOrDefaultAsync(
            new StakeholderByTenantAndIdSpecification(renewal.TenantId, stakeholderId),
            cancellationToken);
        var user = stakeholder is null ? null : await userManager.FindByIdAsync(stakeholder.AppUserId.ToString());
        if (stakeholder is null || string.IsNullOrWhiteSpace(user?.Email))
        {
            logger.LogWarning("Skipped renewal reminder {RenewalId} because its stakeholder recipient has no email.", renewal.Id);
            return;
        }

        await commandSender.SendAsync(
            new SendNotificationCommand(
                renewal.TenantId,
                stakeholder.CountryId,
                NotificationType.SubscriptionRenewalReminder,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    user.Email,
                    new Dictionary<string, string>
                    {
                        ["SubscriptionId"] = subscription.Id.ToString(),
                        ["PeriodEnd"] = renewal.TargetPeriodStartUtc.ToString("O")
                    }))
            {
                StakeholderId = stakeholderId,
                Language = stakeholder.Language
            },
            cancellationToken);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.RenewalReminderScheduled,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.RenewalId] = renewal.Id.ToString()
                }));
    }
}
