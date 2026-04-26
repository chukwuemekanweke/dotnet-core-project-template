using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Infrastructure.Caching;
using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Notifications;
using BackendProjectTemplate.Infrastructure.Observability;
using BackendProjectTemplate.Infrastructure.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using AppDbContext = BackendProjectTemplate.Infrastructure.Persistence.AppDbContext;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;

public abstract class ConsumerWorkerIntegrationTestBase : IAsyncLifetime
{
    private readonly ContainersFixture _fixture;
    private IHost _host = default!;

    protected ConsumerWorkerIntegrationTestBase(ContainersFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await InitializeHostAsync();
        await MigrateDatabaseAsync();
        await InitializeWorkerTestAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await DisposeWorkerTestAsync();
        await DisposeHostAsync();
    }

    protected IServiceScope CreateScope() => _host.Services.CreateScope();

    protected ScopedDbContext CreateDbContextScope() => new(_host.Services.CreateScope());

    protected async Task MigrateDatabaseAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.Database.MigrateAsync();
    }

    protected async Task DisposeHostAsync()
    {
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync();
        _host.Dispose();
    }

    protected virtual Task InitializeWorkerTestAsync() => Task.CompletedTask;

    protected virtual Task DisposeWorkerTestAsync() => Task.CompletedTask;

    protected virtual void RegisterTestServices(IServiceCollection services)
    {
        services.RemoveAll<IMessageContext>();
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        services.AddSingleton(messageContext);
    }

    protected static async Task WaitForConditionAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException("The expected condition was not met in time.");
    }

    private async Task InitializeHostAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["Messaging:RabbitMq:ServiceName"] = "BackendProjectTemplate.Consumer.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = _fixture.RabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = _fixture.RabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = _fixture.RabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = _fixture.RabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = _fixture.RabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = "x.events.backendprojecttemplate.integrationtests",
                ["Messaging:RabbitMq:CommandsExchange"] = "x.commands.backendprojecttemplate.integrationtests",
                ["Notifications:Email:FromAddress"] = "no-reply@integrationtests.local",
                ["Notifications:Email:FromName"] = "BackendProjectTemplate Integration Tests",
                ["OpenTelemetry:ServiceName"] = "BackendProjectTemplate.Consumer.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            })
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<WorkerReadinessState>();
        builder.Services.AddPostgresPersistence(configuration);
        builder.Services.AddIdentityUserManagement(configuration);
        builder.Services.AddAuthenticationServices();
        builder.Services.AddRedisCaching(configuration);
        builder.Services.AddTransactionalOutbox();
        builder.Services.AddNotificationServices(configuration);
        builder.Services.AddCustomTelemetryContext();
        builder.Services.AddBackendTelemetry(configuration);
        builder.Services.AddSubscribers(configuration);
        RegisterTestServices(builder.Services);

        _host = builder.Build();
        await _host.StartAsync();
    }
}
