using BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdministrativelyCancelSubscriptionAtPeriodEnd;

public sealed class When_AdministrativelyCancellingAtPeriodEnd_WithScheduledSubscription_Should
{
    [Fact]
    public async Task ReturnInvalidStateBecauseImmediateCancellationApplies()
    {
        var context = new AdminSubscriptionTestContext();
        var scheduled = Domain.Subscriptions.Entities.Subscription.CreateAllocated(
            context.TenantId, context.TenantBillingAccount.Id, context.Plan, AdminSubscriptionTestContext.Now.AddDays(3), AdminSubscriptionTestContext.Now.AddDays(10),
            AdminSubscriptionTestContext.Now, Guid.CreateVersion7(), context.AdministratorId.ToString(), "future");
        context.ReturnSubscription(scheduled);

        var result = await context.CreatePeriodEndCancellationHandler().HandleAsync(
            new AdministrativelyCancelSubscriptionAtPeriodEndCommand(scheduled.Id, context.TenantId, "reason", context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(AdministrativelyCancelSubscriptionAtPeriodEndStatus.InvalidState);
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
