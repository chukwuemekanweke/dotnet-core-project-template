using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

public sealed class CustomConsumerApplicationFactory(string sqlServerConnectionString, string redisConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = sqlServerConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });
    }
}
