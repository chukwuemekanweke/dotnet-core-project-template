using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithReusedKeyAndDifferentPayload_Should
{
    [Fact]
    public async Task RejectEveryMaterialDifferenceButReplayAFaithfulRetry()
    {
        var context = new AdminSubscriptionTestContext();
        var original = context.CreateAllocation(
            AdminSubscriptionTestContext.Now.AddDays(1).AddTicks(3),
            AdminSubscriptionTestContext.Now.AddDays(5));
        var subscription = SubscriptionAggregate.CreateAllocated(
            context.TenantId, context.TenantBillingAccount.Id, context.Plan, context.Plan.Prices.Single().BillingInterval, original.StartAtUtc!.Value, original.EndAtUtc!.Value,
            AdminSubscriptionTestContext.Now, original.IdempotencyKey, context.AdministratorId.ToString(), original.Reason);
        var allocation = SubscriptionAllocation.Create(
            subscription, context.Plan, original.AllocationSource, original.Reason, context.AdministratorId.ToString(),
            original.IdempotencyKey, AllocateSubscriptionRequestFingerprint.Compute(original));
        context.AllocationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionAllocation>>(), Arg.Any<CancellationToken>())
            .Returns(allocation);
        context.SubscriptionRepository.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        var handler = context.CreateAllocateHandler();

        var variants = new[]
        {
            original with { BillingAccountOwnerType = BillingAccountOwnerType.Stakeholder, StakeholderId = Guid.CreateVersion7() },
            original with { StartAtUtc = original.StartAtUtc.Value.AddHours(1) },
            original with { EndAtUtc = null },
            original with { Reason = "A different justification" },
            original with { TenantId = Guid.CreateVersion7() }
        };
        foreach (var variant in variants)
        {
            (await handler.HandleAsync(variant, CancellationToken.None)).Status.ShouldBe(AllocateSubscriptionStatus.IdempotencyKeyReused);
        }

        var faithful = original with { Reason = $"  {original.Reason} ", StartAtUtc = original.StartAtUtc.Value.AddTicks(-3) };
        (await handler.HandleAsync(faithful, CancellationToken.None)).Status.ShouldBe(AllocateSubscriptionStatus.Replayed);
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
