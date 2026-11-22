using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.DatabaseMigrator;

/// <summary>
/// Runs the database initialization pipeline once and converts its outcome into a process exit code.
/// Exit code <see cref="SuccessExitCode"/> is the only signal deployment orchestration may treat as success.
/// </summary>
public sealed class DatabaseMigrationRunner(
    Func<CancellationToken, Task> initializeDatabase,
    TimeProvider timeProvider,
    ILogger<DatabaseMigrationRunner> logger)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        logger.LogInformation("Database migration started");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await initializeDatabase(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                "Database migration was cancelled after {ElapsedMilliseconds} ms before completing; " +
                "stages that already committed remain applied",
                GetElapsedMilliseconds(startedAt));
            return FailureExitCode;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Database migration failed after {ElapsedMilliseconds} ms; stages that already committed remain applied",
                GetElapsedMilliseconds(startedAt));
            return FailureExitCode;
        }

        logger.LogInformation(
            "Database migration completed successfully in {ElapsedMilliseconds} ms",
            GetElapsedMilliseconds(startedAt));
        return SuccessExitCode;
    }

    private long GetElapsedMilliseconds(long startedAt) =>
        (long)timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
}
