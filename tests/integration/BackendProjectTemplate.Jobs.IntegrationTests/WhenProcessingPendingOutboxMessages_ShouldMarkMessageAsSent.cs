using System.Text.Json;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Microsoft.Extensions.DependencyInjection;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Threading.Channels;

namespace BackendProjectTemplate.Jobs.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenProcessingPendingOutboxMessages_ShouldMarkMessageAsSent
    : JobsIntegrationTestBase, IAsyncLifetime
{
    private readonly string _sqlConnectionString;
    private readonly ContainersFixture _fixture;
    private Guid _messageId;
    private Guid _userId;
    private string _emailAddress = string.Empty;
    private ServiceProvider _subscriberServices = default!;
    private ISubscriber _subscriber = default!;
    private Channel<UserCreated> _channel = default!;

    public WhenProcessingPendingOutboxMessages_ShouldMarkMessageAsSent(ContainersFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
        _sqlConnectionString = fixture.SqlConnectionString;
    }

    public async Task InitializeAsync()
    {
        await GivenTheJobsDatabaseIsMigrated();
        await GivenARealRabbitMqSubscriberIsListeningForPublishedEvents();
        await GivenAPendingOutboxEvent();
        await InitializeClientAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteOutboxRecordsAsync();
        await DisposeSubscriberAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task Verify()
    {
        await WhenTheJobsWorkerProcessesPendingOutboxMessages();
        await ThenThePublishedMessageIsReceivedAndTheOutboxRowIsMarkedAsSent();
    }

    private async Task GivenTheJobsDatabaseIsMigrated()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    private async Task GivenAPendingOutboxEvent()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EfRepository<OutboxMessage>(dbContext);
        _userId = Guid.CreateVersion7();
        _emailAddress = "jobs-outbox@example.com";
        var @event = new UserCreated(_userId, _emailAddress);
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt);

        _messageId = outboxMessage.Id;
        await repository.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();
    }

    private async Task GivenARealRabbitMqSubscriberIsListeningForPublishedEvents()
    {
        var subscriptionName = $"jobs-outbox-{Guid.NewGuid():N}";
        _channel = Channel.CreateUnbounded<UserCreated>();

        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = "BackendProjectTemplate.Jobs.IntegrationTests",
            HostName = _fixture.RabbitMqHostName,
            Port = _fixture.RabbitMqPort,
            UserName = _fixture.RabbitMqUserName,
            Password = _fixture.RabbitMqPassword,
            VirtualHost = _fixture.RabbitMqVirtualHost,
            SubscriptionName = subscriptionName,
            ExchangeName = CustomJobsApplicationFactory.EventsExchange,
            PrefetchCount = 1,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        _subscriberServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_channel)
            .AddSubscriber(subscriberConfig, builder => builder.AddHandler<UserCreated, CapturingUserCreatedHandler>())
            .BuildServiceProvider();

        _subscriber = _subscriberServices.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key);
        await _subscriber.StartAsync(CancellationToken.None);
    }

    private async Task WhenTheJobsWorkerProcessesPendingOutboxMessages()
    {
        var messageReceived = false;
        var messageMarkedSent = false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var received = await _channel.Reader.ReadAsync(cts.Token);
            received.MessageId.ShouldNotBe(Guid.Empty);
            received.UserId.ShouldBe(_userId);
            received.EmailAddress.ShouldBe(_emailAddress);
            messageReceived = true;

            await WaitForConditionAsync(async () =>
            {
                await using var dbContext = CreateDbContext();
                var repository = new EfRepository<OutboxMessage>(dbContext);
                var message = await repository.GetByIdAsync(_messageId);
                return message?.SentAtUtc is not null;
            });

            messageMarkedSent = true;
        }
        finally
        {
            if (!messageReceived || !messageMarkedSent)
            {
                await using var dbContext = CreateDbContext();
                var repository = new EfRepository<OutboxMessage>(dbContext);
                var message = await repository.GetByIdAsync(_messageId);

                throw new InvalidOperationException(
                    $"The outbox message processing did not complete. MessageReceived={messageReceived}, MessageMarkedSent={messageMarkedSent}, AttemptCount={message?.AttemptCount}, LastError={message?.LastError}, SentAtUtc={message?.SentAtUtc?.ToString("O") ?? "<null>"}.");
            }
        }
    }

    private async Task ThenThePublishedMessageIsReceivedAndTheOutboxRowIsMarkedAsSent()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EfRepository<OutboxMessage>(dbContext);
        var message = await repository.GetByIdAsync(_messageId);

        message.ShouldNotBeNull();
        message.SentAtUtc.ShouldNotBeNull();
        message.AttemptCount.ShouldBe(0);
        message.LastError.ShouldBeNull();
    }

    private async Task DeleteOutboxRecordsAsync()
    {
        if (_messageId == Guid.Empty)
        {
            return;
        }

        await using var dbContext = CreateDbContext();
        var repository = new EfRepository<OutboxMessage>(dbContext);
        var message = await repository.GetByIdAsync(_messageId);

        if (message is null)
        {
            return;
        }

        repository.Remove(message);
        await dbContext.SaveChangesAsync();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sqlConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task DisposeSubscriberAsync()
    {
        if (_subscriber is not null)
        {
            await _subscriber.StopAsync(CancellationToken.None);
        }

        if (_subscriberServices is not null)
        {
            await _subscriberServices.DisposeAsync();
        }
    }

    private sealed class CapturingUserCreatedHandler(Channel<UserCreated> channel) : IMessageHandler<UserCreated>
    {
        public Task HandleAsync(UserCreated message, CancellationToken cancellationToken) =>
            channel.Writer.WriteAsync(message, cancellationToken).AsTask();
    }
}
