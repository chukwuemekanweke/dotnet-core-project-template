using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer;

public sealed class Worker(
    ISubscriber subscriber,
    IConsumer consumer,
    ILogger<Worker> logger,
    WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting RabbitMQ subscriber and command consumer.");
        await subscriber.StartAsync(stoppingToken);
        await consumer.StartAsync(stoppingToken);
        readinessState.MarkReady();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            logger.LogInformation("Stopping RabbitMQ subscriber and command consumer.");
            await consumer.StopAsync(CancellationToken.None);
            await subscriber.StopAsync(CancellationToken.None);
        }
    }
}
