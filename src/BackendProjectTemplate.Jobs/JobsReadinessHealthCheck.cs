using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackendProjectTemplate.Jobs;

public sealed class JobsReadinessHealthCheck(WorkerReadinessState readinessState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            readinessState.IsReady
                ? HealthCheckResult.Healthy("Jobs worker is ready to execute scheduled work.")
                : HealthCheckResult.Unhealthy("Jobs worker has not started yet."));
}
