namespace BackendProjectTemplate.Consumer;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consumer worker started. Replace this loop with queue-specific message handling.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Consumer heartbeat at {Timestamp}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
