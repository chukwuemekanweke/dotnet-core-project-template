using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;

public sealed class CustomJobsApplicationFactory(
    string postgresConnectionString,
    string redisConnectionString,
    string rabbitMqHostName,
    int rabbitMqPort,
    string rabbitMqUserName,
    string rabbitMqPassword,
    string rabbitMqVirtualHost) : WebApplicationFactory<Program>
{
    public const string HostEnvironmentName = "IntegrationTests";
    public const string MessagingEnvironment = "integrationtests";
    public const string BaseEventsExchange = "x.events.backendprojecttemplate";
    public const string BaseCommandsExchange = "x.commands.backendprojecttemplate";
    public const string EventsExchange = $"{BaseEventsExchange}.{MessagingEnvironment}";
    public const string CommandsExchange = $"{BaseCommandsExchange}.{MessagingEnvironment}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(HostEnvironmentName);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = postgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = postgresConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Messaging:RabbitMq:ServiceName"] = "BackendProjectTemplate.Jobs.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = rabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = rabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = rabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = rabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = rabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = BaseEventsExchange,
                ["Messaging:RabbitMq:CommandsExchange"] = BaseCommandsExchange,
                [$"{BackendProjectTemplate.Jobs.OutboxProcessing.OutboxProcessingOptions.SectionName}:PollIntervalSeconds"] = "1",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.Jobs.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            });
        });
    }
}
