using BackendProjectTemplate.Jobs.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BackendProjectTemplate.Jobs.UnitTests.HealthChecks;

public sealed class WhenCheckingJobsLiveness_Should
{
    [Fact]
    public async Task ReturnHealthy()
    {
        var healthCheck = new JobsLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}

