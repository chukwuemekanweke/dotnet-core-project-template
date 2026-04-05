using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests;

public sealed class WhenMarkingJobsWorkerReady_ShouldUpdateReadinessState
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
