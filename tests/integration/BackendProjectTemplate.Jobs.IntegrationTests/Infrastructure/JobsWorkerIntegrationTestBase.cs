using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.Jobs.Infrastructure.BackgroundServices;
using BackendProjectTemplate.Jobs.OutboxProcessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;

public abstract class JobsWorkerIntegrationTestBase : IAsyncLifetime
{
    private readonly ContainersFixture _fixture;
    private IHost? _workerHost;

    protected JobsWorkerIntegrationTestBase(ContainersFixture fixture)
    {
        _fixture = fixture;
    }

    protected async Task StartWorkerHostAsync(Action<IServiceCollection, IConfiguration> registerWorkers)
    {
        var configuration = BuildConfiguration();
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddBackgroundServiceReadinessTracking();
        builder.Services.AddSqlServerPersistence(configuration);
        builder.Services.AddRabbitMqOutboxDispatching(configuration);
        RegisterWorkers(builder.Services, configuration);

        _workerHost = builder.Build();
        await _workerHost.StartAsync();
    }

    protected async Task DisposeWorkerHostAsync()
    {
        if (_workerHost is null)
        {
            return;
        }

        await _workerHost.StopAsync();
        _workerHost.Dispose();
    }

    protected async Task MigrateJobsDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    protected AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_fixture.SqlConnectionString)
            .Options;

        return new AppDbContext(options);
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

    public virtual async Task InitializeAsync()
    {
        await MigrateJobsDatabaseAsync();
        await InitializeWorkerTestAsync();
        await StartWorkerHostAsync(RegisterWorkers);
    }

    public virtual async Task DisposeAsync()
    {
        await DisposeWorkerTestAsync();
        await DisposeWorkerHostAsync();
    }

    protected virtual Task InitializeWorkerTestAsync() => Task.CompletedTask;

    protected virtual Task DisposeWorkerTestAsync() => Task.CompletedTask;

    protected abstract void RegisterWorkers(IServiceCollection services, IConfiguration configuration);

    private IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = _fixture.SqlConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                [$"{OutboxProcessingOptions.SectionName}:BatchSize"] = "50",
                [$"{OutboxProcessingOptions.SectionName}:PollIntervalSeconds"] = "1",
                ["Messaging:RabbitMq:ServiceName"] = "BackendProjectTemplate.Jobs.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = _fixture.RabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = _fixture.RabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = _fixture.RabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = _fixture.RabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = _fixture.RabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = CustomJobsApplicationFactory.EventsExchange,
                ["Messaging:RabbitMq:CommandsExchange"] = CustomJobsApplicationFactory.CommandsExchange
            })
            .Build();
}
