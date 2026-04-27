using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments.Services;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory(string postgresConnectionString, string redisConnectionString) : WebApplicationFactory<Program>
{
    public FakeGoogleIdentityTokenService GoogleIdentityTokenService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = postgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = postgresConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Database:InitializeOnStartup"] = "true",
                ["Jwt:Issuer"] = "integration-tests",
                ["Jwt:Audience"] = "integration-tests",
                ["Jwt:SigningKey"] = "super-secret-template-signing-key-change-me",
                ["Authentication:Google:ClientIds:0"] = "integration-tests-google-client-id",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.WebAPI.Tests",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGoogleIdentityTokenService>();
            services.AddSingleton<IGoogleIdentityTokenService>(GoogleIdentityTokenService);
            services.RemoveAll<IPaymentProviderService>();
            services.AddScoped<IPaymentProviderService, FakeCredoPaymentProviderService>();
            services.AddScoped<IPaymentProviderService, FakeSafeHavenPaymentProviderService>();

            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IDistributedCache>();

            var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
            redisConfiguration.AbortOnConnectFail = false;
            var lazyMultiplexer = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfiguration));
            services.AddSingleton<IConnectionMultiplexer>(_ => lazyMultiplexer.Value);
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(lazyMultiplexer.Value);
            });
        });
    }
}
