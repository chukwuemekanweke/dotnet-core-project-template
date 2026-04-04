using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackendProjectTemplate.Jobs;

public sealed class JobsLivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Jobs host is alive."));
}
