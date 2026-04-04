namespace BackendProjectTemplate.Jobs;

public sealed class Worker(
    ILogger<Worker> logger,
    TimeProvider timeProvider,
    WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady();
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("Scheduled job tick at {Timestamp}", timeProvider.GetUtcNow());
        }
    }
}
