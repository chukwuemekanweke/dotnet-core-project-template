using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests;

public sealed class WhenMarkingJobsWorkerReady_ShouldUpdateReadinessState
{
    [Fact]
    public void Verify()
    {
        var state = new WorkerReadinessState();

        state.MarkReady();

        state.IsReady.ShouldBeTrue();
    }
}
