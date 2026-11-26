using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithTenantOwnedBillingAccount_Should
{
    [Fact]
    public async Task ActivateComplimentaryEntitlementAndScheduleExpiryWithoutPaymentRecords()
    {
        var context = new AdminSubscriptionTestContext();
        SubscriptionAggregate? subscription = null;
        SubscriptionAllocation? allocation = null;
        context.SubscriptionRepository.AddAsync(Arg.Do<SubscriptionAggregate>(item => subscription = item), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        context.AllocationRepository.AddAsync(Arg.Do<SubscriptionAllocation>(item => allocation = item), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var command = context.CreateAllocation();

        var result = await context.CreateAllocateHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.Allocated);
        subscription.ShouldNotBeNull();
        allocation.ShouldNotBeNull();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.AutoRenew.ShouldBeFalse();
        subscription.LastSuccessfulPaymentTransactionId.ShouldBeNull();
        subscription.Periods.ShouldHaveSingleItem().PaymentTransactionId.ShouldBeNull();
        subscription.CurrentPeriodEndUtc.ShouldBe(AdminSubscriptionTestContext.Now.AddMonths(1));
        allocation.OperationId.ShouldBe(command.IdempotencyKey);
        allocation.AllocatedByActorId.ShouldBe(context.AdministratorId.ToString());
        result.Response!.AutoRenew.ShouldBeFalse();
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<ExpireSubscriptionCommand>(expiry =>
                expiry.SubscriptionId == subscription.Id &&
                expiry.TenantId == context.TenantId &&
                expiry.ExpectedPeriodEndUtc == subscription.CurrentPeriodEndUtc),
            subscription.CurrentPeriodEndUtc!.Value,
            Arg.Any<CancellationToken>());
        await context.CommandSender.DidNotReceive().ScheduleSendAsync(
            Arg.Any<RenewSubscriptionCommand>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.AuditRecorder.Received(1).RecordAsync(
            Arg.Is<AdministrativeAuditEntry>(entry =>
                entry.Action == AdministrativeAuditActions.Subscriptions.Allocated &&
                entry.TargetType == AdministrativeAuditTargetTypes.Subscription &&
                entry.TargetId == subscription.Id &&
                entry.TargetTenantId == context.TenantId &&
                entry.ActorContext == command.ActorContext &&
                entry.Reason == "Partner onboarding"),
            Arg.Any<CancellationToken>());
    }
}
