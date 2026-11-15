using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithMissingReason_Should
{
    [Fact]
    public async Task RejectInTheDomainWithoutPersisting()
    {
        var context = new AdminSubscriptionTestContext();
        var command = context.CreateAllocation() with { Reason = "   " };

        await Should.ThrowAsync<ArgumentException>(() => context.CreateAllocateHandler().HandleAsync(command, CancellationToken.None));

        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
