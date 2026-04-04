namespace BackendProjectTemplate.Jobs;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("Scheduled job tick at {Timestamp}", DateTimeOffset.UtcNow);
        }
    }
}
