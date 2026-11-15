using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.CancelSubscriptionImmediately;

public sealed class When_CancellingSubscriptionImmediately_WithUnmatchedOrInvalidTarget_Should
{
    [Fact]
    public async Task ReturnNotFoundAlreadyCancelledInvalidStateOrConflict()
    {
        var context = new AdminSubscriptionTestContext();
        var handler = context.CreateImmediateCancellationHandler();
        var subscription = context.CreateActiveAllocation();
        context.ReturnSubscription(subscription);

        (await handler.HandleAsync(new CancelSubscriptionImmediatelyCommand(subscription.Id, Guid.CreateVersion7(), "x", context.Actor), CancellationToken.None))
            .Status.ShouldBe(CancelSubscriptionImmediatelyStatus.NotFound);

        context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new SubscriptionConcurrencyConflictException("changed"));
        (await handler.HandleAsync(new CancelSubscriptionImmediatelyCommand(subscription.Id, context.TenantId, "x", context.Actor), CancellationToken.None))
            .Status.ShouldBe(CancelSubscriptionImmediatelyStatus.ConcurrencyConflict);
        (await handler.HandleAsync(new CancelSubscriptionImmediatelyCommand(subscription.Id, context.TenantId, "x", context.Actor), CancellationToken.None))
            .Status.ShouldBe(CancelSubscriptionImmediatelyStatus.AlreadyCancelled);

        var pending = SubscriptionAggregate.CreatePending(
            context.TenantId, context.TenantBillingAccount.Id, context.Plan.Id, true, AdminSubscriptionTestContext.Now, Guid.CreateVersion7(), null);
        context.ReturnSubscription(pending);
        (await handler.HandleAsync(new CancelSubscriptionImmediatelyCommand(pending.Id, context.TenantId, "x", context.Actor), CancellationToken.None))
            .Status.ShouldBe(CancelSubscriptionImmediatelyStatus.InvalidState);
    }
}
