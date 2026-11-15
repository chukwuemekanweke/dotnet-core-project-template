using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Exceptions;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithCrossTenantStakeholder_Should
{
    [Fact]
    public async Task RejectWithoutPersistingAnything()
    {
        var context = new AdminSubscriptionTestContext();
        var stakeholderId = Guid.CreateVersion7();
        context.BillingAccountResolver.ResolveOrCreateAsync(context.TenantId, BillingAccountOwnerType.Stakeholder, stakeholderId, Arg.Any<CancellationToken>())
            .Returns<BillingAccount>(_ => throw new BillingAccountResolutionException("Stakeholder does not belong to tenant."));

        var result = await context.CreateAllocateHandler().HandleAsync(
            context.CreateAllocation(ownerType: BillingAccountOwnerType.Stakeholder, stakeholderId: stakeholderId),
            CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.OwnerTenantMismatch);
        await context.SubscriptionRepository.DidNotReceive().AddAsync(Arg.Any<SubscriptionAggregate>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
