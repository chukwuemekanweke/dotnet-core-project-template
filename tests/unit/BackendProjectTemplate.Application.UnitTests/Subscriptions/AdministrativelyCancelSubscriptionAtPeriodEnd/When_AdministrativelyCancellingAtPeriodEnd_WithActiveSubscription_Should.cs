using BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdministrativelyCancelSubscriptionAtPeriodEnd;

public sealed class When_AdministrativelyCancellingAtPeriodEnd_WithActiveSubscription_Should
{
    [Fact]
    public async Task KeepEntitlementAndScheduleExpiryForTheSubscriptionTenant()
    {
        var context = new AdminSubscriptionTestContext();
        var subscription = context.CreateActiveAllocation();
        context.ReturnSubscription(subscription);
        var handler = context.CreatePeriodEndCancellationHandler();
        var command = new AdministrativelyCancelSubscriptionAtPeriodEndCommand(subscription.Id, context.TenantId, "Contract ended", context.Actor);

        var first = await handler.HandleAsync(command, CancellationToken.None);
        var repeated = await handler.HandleAsync(command, CancellationToken.None);

        first.Status.ShouldBe(AdministrativelyCancelSubscriptionAtPeriodEndStatus.Scheduled);
        first.Subscription!.Status.ShouldBe(nameof(SubscriptionStatus.Active));
        first.Subscription.CancelAtPeriodEnd.ShouldBeTrue();
        first.Subscription.CancellationReason.ShouldBe("Contract ended");
        repeated.Status.ShouldBe(AdministrativelyCancelSubscriptionAtPeriodEndStatus.AlreadyScheduled);
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<ExpireSubscriptionCommand>(expiry => expiry.SubscriptionId == subscription.Id && expiry.TenantId == context.TenantId),
            subscription.CurrentPeriodEndUtc!.Value,
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
