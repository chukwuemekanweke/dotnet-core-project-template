using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
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

        var rabbitMqHostName = configuration["Messaging:RabbitMq:HostName"];
        if (string.IsNullOrWhiteSpace(rabbitMqHostName))
        {
            return HealthCheckResult.Unhealthy("Consumer RabbitMQ host name is not configured.");
        }

        var rabbitMqUserName = configuration["Messaging:RabbitMq:UserName"];
        if (string.IsNullOrWhiteSpace(rabbitMqUserName))
        {
            return HealthCheckResult.Unhealthy("Consumer RabbitMQ user name is not configured.");
        }

        var rabbitMqPassword = configuration["Messaging:RabbitMq:Password"];
        if (string.IsNullOrWhiteSpace(rabbitMqPassword))
        {
            return HealthCheckResult.Unhealthy("Consumer RabbitMQ password is not configured.");
        }

        var rabbitMqVirtualHost = configuration["Messaging:RabbitMq:VirtualHost"];
        if (string.IsNullOrWhiteSpace(rabbitMqVirtualHost))
        {
            return HealthCheckResult.Unhealthy("Consumer RabbitMQ virtual host is not configured.");
        }

        if (!int.TryParse(configuration["Messaging:RabbitMq:Port"], out var rabbitMqPort) || rabbitMqPort <= 0)
        {
            return HealthCheckResult.Unhealthy("Consumer RabbitMQ port is not configured correctly.");
        }

        try
        {
            await using var sqlConnection = new SqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            await using var sqlCommand = new SqlCommand("SELECT 1", sqlConnection);
            await sqlCommand.ExecuteScalarAsync(cancellationToken);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            await redis.GetDatabase().PingAsync();

            var connectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqHostName,
                Port = rabbitMqPort,
                UserName = rabbitMqUserName,
                Password = rabbitMqPassword,
                VirtualHost = rabbitMqVirtualHost
            };

            await using var rabbitMqConnection = await connectionFactory.CreateConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy("Consumer worker can connect to SQL Server, Redis, and RabbitMQ.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Consumer worker cannot connect to one or more required dependencies.",
                exception);
        }
    }
}
