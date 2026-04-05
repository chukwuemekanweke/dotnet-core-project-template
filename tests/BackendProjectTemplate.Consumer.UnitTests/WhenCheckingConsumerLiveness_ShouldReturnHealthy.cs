using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests;

public sealed class WhenCheckingConsumerLiveness_ShouldReturnHealthy
{
    [Fact]
    public async Task Verify()
    {
        var healthCheck = new ConsumerLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}
