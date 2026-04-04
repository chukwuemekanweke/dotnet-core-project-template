using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BackendProjectTemplate.Consumer;

public sealed class ConsumerReadinessHealthCheck(
    IConfiguration configuration,
    WorkerReadinessState readinessState) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!readinessState.IsReady)
        {
            return HealthCheckResult.Unhealthy("Consumer worker has not started yet.");
        }

        var sqlConnectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            return HealthCheckResult.Unhealthy("Consumer SQL Server connection string is not configured.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return HealthCheckResult.Unhealthy("Consumer Redis connection string is not configured.");
        }

        try
        {
            await using var sqlConnection = new SqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            await using var sqlCommand = new SqlCommand("SELECT 1", sqlConnection);
            await sqlCommand.ExecuteScalarAsync(cancellationToken);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            await redis.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy("Consumer worker can connect to SQL Server and Redis.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Consumer worker cannot connect to one or more required dependencies.",
                exception);
        }
    }
}
