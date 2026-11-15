using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithDuplicateIdempotencyKey_Should
{
    [Fact]
    public async Task ReturnTheOriginalAllocationWithoutRepeatingSideEffects()
    {
        var context = new AdminSubscriptionTestContext();
        var command = context.CreateAllocation();
        var original = SubscriptionAggregate.CreateAllocated(
            context.TenantId, context.TenantBillingAccount.Id, context.Plan, AdminSubscriptionTestContext.Now, AdminSubscriptionTestContext.Now.AddDays(10),
            AdminSubscriptionTestContext.Now, command.IdempotencyKey, context.AdministratorId.ToString(), command.Reason);
        var allocation = SubscriptionAllocation.Create(
            original, context.Plan, command.AllocationSource, command.Reason, context.AdministratorId.ToString(), command.IdempotencyKey, AllocateSubscriptionRequestFingerprint.Compute(command));
        context.AllocationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionAllocation>>(), Arg.Any<CancellationToken>())
            .Returns(allocation);
        context.SubscriptionRepository.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

        var replay = await context.CreateAllocateHandler().HandleAsync(command, CancellationToken.None);
        var reused = await context.CreateAllocateHandler().HandleAsync(
            command with { AllocationSource = SubscriptionAllocationSource.Trial },
            CancellationToken.None);

        replay.Status.ShouldBe(AllocateSubscriptionStatus.Replayed);
        replay.Response!.SubscriptionId.ShouldBe(original.Id);
        replay.Response.AllocationId.ShouldBe(allocation.Id);
        reused.Status.ShouldBe(AllocateSubscriptionStatus.IdempotencyKeyReused);
        await context.SubscriptionRepository.DidNotReceive().AddAsync(Arg.Any<SubscriptionAggregate>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.CommandSender.DidNotReceiveWithAnyArgs().ScheduleSendAsync<ExpireSubscriptionCommand>(default!, default, default);
    }
}
