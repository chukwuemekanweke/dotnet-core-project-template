using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BackendProjectTemplate.Consumer;

public sealed class Worker(
    ISubscriber subscriber,
    ILogger<Worker> logger,
    WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting RabbitMQ subscriber.");
        await subscriber.StartAsync(stoppingToken);
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
            logger.LogInformation("Stopping RabbitMQ subscriber.");
            await subscriber.StopAsync(CancellationToken.None);
        }
    }
}
