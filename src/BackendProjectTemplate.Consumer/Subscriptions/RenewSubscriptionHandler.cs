using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Options;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.Subscriptions;

public sealed class RenewSubscriptionHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionAggregate> subscriptionRepository,
    IRepository<SubscriptionRenewal> renewalRepository,
    IRepository<RecurringPaymentAuthorization> authorizationRepository,
    ICommandSender commandSender,
    IOptions<SubscriptionRenewalOptions> options,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<RenewSubscriptionHandler> logger) : BaseMessageHandler<RenewSubscriptionCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(RenewSubscriptionCommand message, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FirstOrDefaultAsync(
            new SubscriptionForFulfillmentSpecification(message.SubscriptionId),
            cancellationToken);
        if (subscription is null ||
            subscription.TenantId != message.TenantId ||
            !subscription.AutoRenew ||
            subscription.CancelAtPeriodEnd ||
            subscription.CurrentPeriodEndUtc != message.TargetPeriodStartUtc)
        {
            return;
        }

        var now = Clock.GetUtcNow();
        if (subscription.NextBillingAtUtc.HasValue && now < subscription.NextBillingAtUtc.Value)
        {
            await commandSender.ScheduleSendAsync(
                new RenewSubscriptionCommand(subscription.Id, message.TargetPeriodStartUtc),
                subscription.NextBillingAtUtc.Value,
                cancellationToken);
            return;
        }

        var renewal = await renewalRepository.FirstOrDefaultAsync(
            new SubscriptionRenewalByIdentitySpecification(subscription.Id, message.TargetPeriodStartUtc),
            cancellationToken);
        if (renewal is not null)
        {
            return;
        }

        var currentPeriod = subscription.Periods.SingleOrDefault(period => period.PeriodEndUtc == message.TargetPeriodStartUtc);
        if (currentPeriod is null)
        {
            return;
        }

        renewal = SubscriptionRenewal.Create(subscription, currentPeriod, now);
        await renewalRepository.AddAsync(renewal, cancellationToken);

        var authorization = await authorizationRepository.FirstOrDefaultAsync(
            new UsableRecurringPaymentAuthorizationSpecification(subscription.TenantId, subscription.BillingAccountId, now),
            cancellationToken);

        // None of the currently integrated providers exposes a verified merchant-initiated recurring
        // charge capability. Even if an authorization row is provisioned ahead of an adapter, fail
        // closed instead of treating a one-time provider reference as a reusable mandate.
        renewal.MarkAwaitingAuthorization();
        for (var index = 0; index < options.Value.ReminderSchedule.Length; index++)
        {
            await commandSender.ScheduleSendAsync(
                new SendSubscriptionRenewalReminderCommand(subscription.Id, renewal.Id, index + 1),
                message.TargetPeriodStartUtc.Add(options.Value.ReminderSchedule[index]),
                cancellationToken);
        }
        await commandSender.ScheduleSendAsync(
            new ExpireSubscriptionCommand(subscription.Id, message.TargetPeriodStartUtc),
            message.TargetPeriodStartUtc.Add(options.Value.GracePeriod),
            cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Subscriptions.RenewalAwaitingAuthorization,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Subscriptions.SubscriptionId] = subscription.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.RenewalId] = renewal.Id.ToString(),
                    [Observability.PropertyNames.Subscriptions.BillingAccountId] = subscription.BillingAccountId.ToString()
                }));

        logger.LogWarning(
            "Subscription renewal {RenewalId} was not charged because no supported reusable authorization adapter is available. AuthorizationPresent={AuthorizationPresent}",
            renewal.Id,
            authorization is not null);
    }
}
