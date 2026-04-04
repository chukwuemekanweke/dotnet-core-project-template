using BackendProjectTemplate.Domain.Common.Authentication;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithPassword("Your_strong_Password123!")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public TestOtpDeliveryService OtpDeliveryService =>
        Services.GetRequiredService<TestOtpDeliveryService>();

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
                ["Database:InitializeOnStartup"] = "true",
                ["Jwt:Issuer"] = "integration-tests",
                ["Jwt:Audience"] = "integration-tests",
                ["Jwt:SigningKey"] = "super-secret-template-signing-key-change-me",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.WebAPI.Tests",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOtpDeliveryService>();
            services.AddSingleton<TestOtpDeliveryService>();
            services.AddSingleton<IOtpDeliveryService>(provider => provider.GetRequiredService<TestOtpDeliveryService>());
        });
    }
}
