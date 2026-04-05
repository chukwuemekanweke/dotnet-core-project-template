using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests.Infrastructure.BackgroundServices;

public sealed class WhenMarkingBackgroundServiceReady_ShouldUpdateReadinessState
{
    [Fact]
    public void Verify()
    {
        var state = new BackgroundServiceReadinessState(
            [new BackgroundServiceDescriptor("OutboxMessageProcessor")]);

        state.MarkReady("OutboxMessageProcessor");

        state.IsReady.ShouldBeTrue();
    }
}
