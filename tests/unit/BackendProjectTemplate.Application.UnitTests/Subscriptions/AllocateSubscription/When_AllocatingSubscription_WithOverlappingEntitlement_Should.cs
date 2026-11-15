using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithOverlappingEntitlement_Should
{
    [Fact]
    public async Task RejectWithConflictFromTheApplicationCheck()
    {
        var context = new AdminSubscriptionTestContext();
        context.SubscriptionRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await context.CreateAllocateHandler().HandleAsync(context.CreateAllocation(), CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.EntitlementConflict);
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWithConflictWhenTheDatabaseExclusionConstraintWins()
    {
        var context = new AdminSubscriptionTestContext();
        context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new SubscriptionEntitlementConflictException("overlap"));

        var result = await context.CreateAllocateHandler().HandleAsync(context.CreateAllocation(), CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.EntitlementConflict);
        result.Response.ShouldBeNull();
    }
}
