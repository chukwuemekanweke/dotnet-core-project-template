using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.RenewSubscription;

public sealed class When_HandlingRenewSubscription_WithImmediatelyCancelledSubscription_Should
{
    [Fact]
    public async Task NotCreateARenewalOrChargeAttempt()
    {
        var now = SubscriptionsConsumerTestContext.AllocatedAt;
        var context = new SubscriptionsConsumerTestContext(now.AddMonths(1));
        var stakeholderId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var subscription = SubscriptionAggregate.CreatePending(
            context.TenantId, context.BillingAccountId, context.Plan.Id, context.Plan.Prices.Single().BillingInterval, true, now, Guid.CreateVersion7(), stakeholderId.ToString());
        var purchase = SubscriptionPurchase.Create(context.TenantId, context.BillingAccountId, subscription.Id, context.Plan, context.Plan.Prices.Single().BillingInterval, paymentId, stakeholderId);
        subscription.ActivateFromPayment(purchase, paymentId, now, purchase.Id);
        var renewalBoundary = subscription.CurrentPeriodEndUtc!.Value;
        subscription.CancelImmediately(now.AddDays(5), Guid.CreateVersion7(), context.ActorId, "Chargeback");
        context.SubscriptionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        await context.CreateRenewHandler().HandleAsync(
            new RenewSubscriptionCommand(subscription.Id, renewalBoundary) { TenantId = context.TenantId },
            CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
        await context.RenewalRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await context.CommandSender.DidNotReceiveWithAnyArgs().ScheduleSendAsync<ExpireSubscriptionCommand>(default!, default, default);
    }
}
