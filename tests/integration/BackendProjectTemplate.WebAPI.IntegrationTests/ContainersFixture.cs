using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit.Sdk;

namespace BackendProjectTemplate.WebAPI.IntegrationTests;

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
                InitialCatalog = $"backend_template_webapi_tests_{Guid.CreateVersion7():N}",
                TrustServerCertificate = true
            }.ConnectionString;

            RedisConnectionString = Redis.GetConnectionString();
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw SkipException.ForSkip("Docker is unavailable. WebAPI integration tests require Docker to run.");
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
}
