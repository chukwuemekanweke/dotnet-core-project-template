using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory(string sqlServerConnectionString, string redisConnectionString) : WebApplicationFactory<Program>
{
    public TestOtpDeliveryService OtpDeliveryService =>
        Services.GetRequiredService<TestOtpDeliveryService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = sqlServerConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
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
