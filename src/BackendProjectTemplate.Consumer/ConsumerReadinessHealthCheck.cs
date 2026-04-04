using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackendProjectTemplate.Consumer;

public sealed class ConsumerReadinessHealthCheck(WorkerReadinessState readinessState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            readinessState.IsReady
                ? HealthCheckResult.Healthy("Consumer worker is ready to process messages.")
                : HealthCheckResult.Unhealthy("Consumer worker has not started yet."));
}
