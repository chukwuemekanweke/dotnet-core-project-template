using BackendProjectTemplate.Jobs.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests.HealthChecks;

public sealed class WhenCheckingJobsLiveness_ShouldReturnHealthy
{
    [Fact]
    public async Task Verify()
    {
        var healthCheck = new JobsLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}
