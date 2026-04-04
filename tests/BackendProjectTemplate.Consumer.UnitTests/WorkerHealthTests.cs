using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WorkerHealthTests
{
    [Fact]
    public async Task LivenessHealthCheck_ReturnsHealthy()
    {
        var healthCheck = new ConsumerLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void ReadinessState_MarksWorkerAsReady()
    {
        var state = new WorkerReadinessState();

        state.MarkReady();

        state.IsReady.ShouldBeTrue();
    }
}
