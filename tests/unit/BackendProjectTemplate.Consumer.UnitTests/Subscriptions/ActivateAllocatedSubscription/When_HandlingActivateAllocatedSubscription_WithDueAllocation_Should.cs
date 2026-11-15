using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions.ActivateAllocatedSubscription;

public sealed class When_HandlingActivateAllocatedSubscription_WithDueAllocation_Should
{
    [Fact]
    public async Task ActivateAndScheduleExpiry()
    {
        var start = SubscriptionsConsumerTestContext.AllocatedAt.AddDays(3);
        var context = new SubscriptionsConsumerTestContext(start);
        var (subscription, allocation) = context.CreateScheduledAllocation(start, start.AddDays(30));
        var command = context.CreateActivation(subscription, allocation);

        await context.CreateActivationHandler().HandleAsync(command, CancellationToken.None);

        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.CurrentPeriodStartUtc.ShouldBe(start);
        await context.LifecyclePersistence.Received(1).AddPeriodAsync(
            Arg.Is<SubscriptionPeriod>(period => period.PaymentTransactionId == null && period.OperationId == allocation.OperationId),
            Arg.Any<CancellationToken>());
        await context.LifecyclePersistence.Received(1).AddStatusHistoryAsync(
            Arg.Is<SubscriptionStatusHistory>(history => history.OperationId == command.MessageId && history.NewStatus == SubscriptionStatus.Active),
            Arg.Any<CancellationToken>());
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<ExpireSubscriptionCommand>(expiry => expiry.SubscriptionId == subscription.Id && expiry.TenantId == context.TenantId),
            allocation.EndAtUtc,
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
