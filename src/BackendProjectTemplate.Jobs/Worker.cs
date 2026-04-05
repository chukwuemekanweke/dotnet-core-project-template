using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Jobs;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxProcessingOptions> options,
    WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));

        do
        {
            await ProcessPendingMessagesAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();
        var now = timeProvider.GetUtcNow();

        var messages = await repository.ListAsync(
            new PendingOutboxMessagesSpecification(options.Value.BatchSize),
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);
                message.MarkSent(now);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to dispatch outbox message {MessageId} of type {MessageType}",
                    message.MessageId,
                    message.Type);

                message.MarkAttempt(now, exception.Message);
            }

            repository.Update(message);
        }

        if (messages.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class PendingOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public PendingOutboxMessagesSpecification(int batchSize)
        {
            Where(message => message.SentAtUtc == null);
            ApplyOrderBy(message => message.EnqueuedAtUtc);
            ApplyPaging(0, batchSize);
            EnableTracking();
        }
    }
}
