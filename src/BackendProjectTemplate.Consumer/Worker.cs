namespace BackendProjectTemplate.Consumer;

public sealed class Worker(
    ILogger<Worker> logger,
    TimeProvider timeProvider,
    WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady();
        logger.LogInformation("Consumer worker started. Replace this loop with queue-specific message handling.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Consumer heartbeat at {Timestamp}", timeProvider.GetUtcNow());
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
