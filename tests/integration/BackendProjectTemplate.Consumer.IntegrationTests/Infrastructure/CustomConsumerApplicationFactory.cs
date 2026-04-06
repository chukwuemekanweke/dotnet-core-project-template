using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

public sealed class CustomConsumerApplicationFactory(
    string sqlServerConnectionString,
    string redisConnectionString,
    string rabbitMqHostName,
    int rabbitMqPort,
    string rabbitMqUserName,
    string rabbitMqPassword,
    string rabbitMqVirtualHost) : WebApplicationFactory<Program>
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
                ["Messaging:RabbitMq:ServiceName"] = "BackendProjectTemplate.Consumer.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = rabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = rabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = rabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = rabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = rabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = "x.events.backendprojecttemplate.integrationtests",
                ["Messaging:RabbitMq:CommandsExchange"] = "x.commands.backendprojecttemplate.integrationtests",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.Consumer.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOtpDeliveryService>();
            services.AddSingleton<TestOtpDeliveryService>();
            services.AddSingleton<IOtpDeliveryService>(provider => provider.GetRequiredService<TestOtpDeliveryService>());
            services.RemoveAll<IHostedService>();
            services.AddHostedService<TestReadyWorker>();
        });
    }
}
