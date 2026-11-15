using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.CancelSubscriptionImmediately;

public sealed class When_CancellingSubscriptionImmediately_WithActiveSubscription_Should
{
    [Fact]
    public async Task EndEntitlementPersistHistoryAndCancelOpenRenewals()
    {
        var context = new AdminSubscriptionTestContext();
        var subscription = context.CreateActiveAllocation();
        context.ReturnSubscription(subscription);
        var openRenewal = SubscriptionRenewal.Create(subscription, subscription.Periods.Single(), AdminSubscriptionTestContext.Now);
        context.RenewalRepository.ListAsync(Arg.Any<ISpecification<SubscriptionRenewal>>(), Arg.Any<CancellationToken>())
            .Returns([openRenewal]);

        var result = await context.CreateImmediateCancellationHandler().HandleAsync(
            new CancelSubscriptionImmediatelyCommand(subscription.Id, context.TenantId, "Fraud review", context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(CancelSubscriptionImmediatelyStatus.Cancelled);
        result.Subscription!.Status.ShouldBe(nameof(SubscriptionStatus.Cancelled));
        result.Subscription.EndedAtUtc.ShouldBe(AdminSubscriptionTestContext.Now);
        result.Subscription.CancellationReason.ShouldBe("Fraud review");
        openRenewal.Status.ShouldBe(SubscriptionRenewalStatus.Cancelled);
        await context.LifecyclePersistence.Received(1).AddStatusHistoryAsync(
            Arg.Is<SubscriptionStatusHistory>(history =>
                history.NewStatus == SubscriptionStatus.Cancelled &&
                history.ActorId == context.AdministratorId.ToString() &&
                history.Details == "Fraud review"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
