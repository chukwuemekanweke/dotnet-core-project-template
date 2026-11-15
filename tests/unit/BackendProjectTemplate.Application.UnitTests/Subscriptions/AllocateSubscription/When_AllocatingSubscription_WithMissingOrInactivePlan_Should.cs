using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithMissingOrInactivePlan_Should
{
    [Fact]
    public async Task RejectBeforeResolvingTheBillingAccount()
    {
        var missing = new AdminSubscriptionTestContext();
        missing.PlanRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionPlan?)null);
        var inactive = new AdminSubscriptionTestContext();
        inactive.Plan.SetActive(false);

        (await missing.CreateAllocateHandler().HandleAsync(missing.CreateAllocation(), CancellationToken.None))
            .Status.ShouldBe(AllocateSubscriptionStatus.PlanNotFound);
        (await inactive.CreateAllocateHandler().HandleAsync(inactive.CreateAllocation(), CancellationToken.None))
            .Status.ShouldBe(AllocateSubscriptionStatus.PlanInactive);
        await missing.BillingAccountResolver.DidNotReceiveWithAnyArgs().ResolveOrCreateAsync(default, default, default, default);
        await inactive.BillingAccountResolver.DidNotReceiveWithAnyArgs().ResolveOrCreateAsync(default, default, default, default);
    }
}
