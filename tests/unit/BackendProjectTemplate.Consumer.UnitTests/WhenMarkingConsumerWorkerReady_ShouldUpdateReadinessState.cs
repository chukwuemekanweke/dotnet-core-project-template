using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenMarkingConsumerWorkerReady_ShouldUpdateReadinessState
{
    [Fact]
    public void Verify()
    {
        var state = new WorkerReadinessState();

        state.MarkReady();

        state.IsReady.ShouldBeTrue();
    }
}
