using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithFutureStart_Should
{
    [Fact]
    public async Task ScheduleActivationThroughTheOutboxForTheTargetTenant()
    {
        var context = new AdminSubscriptionTestContext();
        var start = AdminSubscriptionTestContext.Now.AddDays(5);
        var end = start.AddDays(14);

        var result = await context.CreateAllocateHandler().HandleAsync(context.CreateAllocation(start, end), CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.Allocated);
        result.Response!.Status.ShouldBe(nameof(SubscriptionStatus.Scheduled));
        result.Response.EntitlementStartUtc.ShouldBe(start);
        result.Response.EntitlementEndUtc.ShouldBe(end);
        await context.CommandSender.Received(1).ScheduleSendAsync(
            Arg.Is<ActivateAllocatedSubscriptionCommand>(activation =>
                activation.SubscriptionId == result.Response.SubscriptionId &&
                activation.AllocationId == result.Response.AllocationId &&
                activation.ExpectedStartUtc == start &&
                activation.TenantId == context.TenantId),
            start,
            Arg.Any<CancellationToken>());
        await context.CommandSender.DidNotReceiveWithAnyArgs().ScheduleSendAsync<ExpireSubscriptionCommand>(default!, default, default);
    }
}
