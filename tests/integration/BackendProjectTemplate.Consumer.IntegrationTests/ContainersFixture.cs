using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit.Sdk;

namespace BackendProjectTemplate.Consumer.IntegrationTests;

[CollectionDefinition(nameof(ContainersCollection))]
public sealed class ContainersCollection : ICollectionFixture<ContainersFixture>;

public sealed class ContainersFixture : IAsyncLifetime
{
    public MsSqlContainer SqlServer { get; private set; } = default!;
    public RedisContainer Redis { get; private set; } = default!;
    public string SqlConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            SqlServer = new MsSqlBuilder()
                .WithPassword("Your_strong_Password123!")
                .Build();

            Redis = new RedisBuilder().Build();

            await Task.WhenAll(SqlServer.StartAsync(), Redis.StartAsync());

            SqlConnectionString = new SqlConnectionStringBuilder(SqlServer.GetConnectionString())
            {
                InitialCatalog = $"backend_template_consumer_tests_{Guid.NewGuid():N}",
                TrustServerCertificate = true
            }.ConnectionString;

            await EnsureDatabaseExistsAsync(SqlConnectionString);
            RedisConnectionString = Redis.GetConnectionString();
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw SkipException.ForSkip("Docker is unavailable. Consumer integration tests require Docker to run.");
        }
    }

    public async Task DisposeAsync()
    {
        var tasks = new List<Task>(2);

        if (Redis is not null)
        {
            tasks.Add(Redis.DisposeAsync().AsTask());
        }

        if (SqlServer is not null)
        {
            tasks.Add(SqlServer.DisposeAsync().AsTask());
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{databaseName}') IS NULL CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }
}
