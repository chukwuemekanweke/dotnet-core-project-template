using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithInvalidPeriod_Should
{
    [Fact]
    public async Task RejectPastStartsAndNonPositiveWindows()
    {
        var context = new AdminSubscriptionTestContext();
        var handler = context.CreateAllocateHandler();

        (await handler.HandleAsync(context.CreateAllocation(AdminSubscriptionTestContext.Now.AddHours(-1)), CancellationToken.None))
            .Status.ShouldBe(AllocateSubscriptionStatus.InvalidPeriod);
        (await handler.HandleAsync(context.CreateAllocation(endAtUtc: AdminSubscriptionTestContext.Now), CancellationToken.None))
            .Status.ShouldBe(AllocateSubscriptionStatus.InvalidPeriod);
        var skewed = await handler.HandleAsync(context.CreateAllocation(AdminSubscriptionTestContext.Now.AddMinutes(-2)), CancellationToken.None);
        skewed.Status.ShouldBe(AllocateSubscriptionStatus.Allocated);
        skewed.Response!.EntitlementStartUtc.ShouldBe(AdminSubscriptionTestContext.Now);
    }
}
