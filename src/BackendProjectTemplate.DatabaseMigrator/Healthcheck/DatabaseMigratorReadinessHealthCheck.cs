using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackendProjectTemplate.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigratorReadinessHealthCheck(DatabaseMigrationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.Status switch
        {
            DatabaseMigrationStatus.Failed => HealthCheckResult.Unhealthy(
                "Database migrator failed before completing deployment.",
                state.Failure),
            DatabaseMigrationStatus.Pending => HealthCheckResult.Healthy("Database migrator is ready to start deployment work."),
            DatabaseMigrationStatus.Running => HealthCheckResult.Healthy("Database migrator is currently running deployment work."),
            DatabaseMigrationStatus.Succeeded => HealthCheckResult.Healthy("Database migrator completed deployment work successfully."),
            _ => HealthCheckResult.Unhealthy("Database migrator is in an unknown state.")
        });
}
