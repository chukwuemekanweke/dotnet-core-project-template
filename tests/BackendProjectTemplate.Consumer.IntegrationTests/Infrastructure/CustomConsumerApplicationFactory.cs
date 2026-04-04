using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

public sealed class CustomConsumerApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithPassword("Your_strong_Password123!")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        await _redis.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlServer.DisposeAsync().AsTask();
        await _redis.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = _sqlServer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });
    }
}
