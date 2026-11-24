using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithConcurrentWinnerBeforeOverlapCheck_Should
{
    [Fact]
    public async Task ReplayTheCommittedWinner()
    {
        var context = new AdminSubscriptionTestContext();
        var command = context.CreateAllocation();
        var winner = SubscriptionAggregate.CreateAllocated(
            context.TenantId,
            context.TenantBillingAccount.Id,
            context.Plan,
            context.Plan.Prices.Single().BillingInterval,
            AdminSubscriptionTestContext.Now,
            AdminSubscriptionTestContext.Now.AddDays(10),
            AdminSubscriptionTestContext.Now,
            command.IdempotencyKey,
            context.AdministratorId.ToString(),
            command.Reason);
        var winningAllocation = SubscriptionAllocation.Create(
            winner,
            context.Plan,
            command.AllocationSource,
            command.Reason,
            context.AdministratorId.ToString(),
            command.IdempotencyKey,
            AllocateSubscriptionRequestFingerprint.Compute(command));
        context.AllocationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SubscriptionAllocation>>(),
                Arg.Any<CancellationToken>())
            .Returns((SubscriptionAllocation?)null, winningAllocation);
        context.SubscriptionRepository.AnyAsync(
                Arg.Any<ISpecification<SubscriptionAggregate>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        context.SubscriptionRepository.GetByIdAsync(winner.Id, Arg.Any<CancellationToken>()).Returns(winner);

        var result = await context.CreateAllocateHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.Replayed);
        result.Response!.SubscriptionId.ShouldBe(winner.Id);
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
