using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BackendProjectTemplate.Jobs.HealthChecks;

public sealed class JobsReadinessHealthCheck(
    IConfiguration configuration,
    BackgroundServiceReadinessState readinessState) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!readinessState.IsReady)
        {
            var pendingServices = readinessState.PendingServices;
            var description = pendingServices.Count == 0
                ? "No Jobs background services are registered for readiness tracking."
                : $"Jobs background services have not started yet: {string.Join(", ", pendingServices)}.";

            return HealthCheckResult.Unhealthy(description);
        }

        var sqlConnectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            return HealthCheckResult.Unhealthy("Jobs SQL Server connection string is not configured.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return HealthCheckResult.Unhealthy("Jobs Redis connection string is not configured.");
        }

        try
        {
            await using var sqlConnection = new SqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            await using var sqlCommand = new SqlCommand("SELECT 1", sqlConnection);
            await sqlCommand.ExecuteScalarAsync(cancellationToken);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            await redis.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy("Jobs host can connect to SQL Server and Redis.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Jobs host cannot connect to one or more required dependencies.",
                exception);
        }
    }
}
