using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithMultiIntervalPlan_Should
{
    [Fact]
    public async Task RequireAnOfferedIntervalAndSnapshotItsPriceAndPeriod()
    {
        var context = new AdminSubscriptionTestContext();
        var plan = SubscriptionPlan.Create(
            "STANDARD",
            "Standard",
            Guid.CreateVersion7(),
            new Dictionary<BillingInterval, decimal> { [BillingInterval.Monthly] = 1500000m, [BillingInterval.Yearly] = 15000000m },
            true);
        context.PlanRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>())
            .Returns(plan);
        SubscriptionAllocation? captured = null;
        context.AllocationRepository
            .When(repository => repository.AddAsync(Arg.Any<SubscriptionAllocation>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<SubscriptionAllocation>());
        var handler = context.CreateAllocateHandler();

        var ambiguous = await handler.HandleAsync(
            context.CreateAllocation() with { PlanId = plan.Id },
            CancellationToken.None);
        var yearly = await handler.HandleAsync(
            context.CreateAllocation() with { PlanId = plan.Id, EndAtUtc = null, BillingInterval = BillingInterval.Yearly },
            CancellationToken.None);

        ambiguous.Status.ShouldBe(AllocateSubscriptionStatus.BillingIntervalNotOffered);
        yearly.Status.ShouldBe(AllocateSubscriptionStatus.Allocated, yearly.Error);
        captured.ShouldNotBeNull();
        captured.BillingInterval.ShouldBe(BillingInterval.Yearly);
        captured.NominalPrice.ShouldBe(15000000m);
        captured.EndAtUtc.ShouldBe(captured.StartAtUtc.AddYears(1));
    }

    [Fact]
    public async Task UseTheOnlyIntervalOfASingleIntervalPlanAndRejectOthers()
    {
        var context = new AdminSubscriptionTestContext();
        var handler = context.CreateAllocateHandler();

        var implicitMonthly = await handler.HandleAsync(context.CreateAllocation(), CancellationToken.None);
        var unoffered = await new AdminSubscriptionTestContext().CreateAllocateHandler().HandleAsync(
            context.CreateAllocation() with { BillingInterval = BillingInterval.Yearly },
            CancellationToken.None);

        implicitMonthly.Status.ShouldBe(AllocateSubscriptionStatus.Allocated, implicitMonthly.Error);
        unoffered.Status.ShouldBe(AllocateSubscriptionStatus.BillingIntervalNotOffered);
    }
}
