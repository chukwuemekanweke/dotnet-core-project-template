using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory(string sqlServerConnectionString, string redisConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServerWrite"] = sqlServerConnectionString,
                ["ConnectionStrings:SqlServerRead"] = sqlServerConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Database:InitializeOnStartup"] = "true",
                ["Jwt:Issuer"] = "integration-tests",
                ["Jwt:Audience"] = "integration-tests",
                ["Jwt:SigningKey"] = "super-secret-template-signing-key-change-me",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.WebAPI.Tests",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });
    }
}
