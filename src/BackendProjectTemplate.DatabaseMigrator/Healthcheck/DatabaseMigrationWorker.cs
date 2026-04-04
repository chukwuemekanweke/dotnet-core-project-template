using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigrationWorker(
    IServiceProvider services,
    IHostEnvironment environment,
    IOptions<DatabaseMigratorHealthcheckOptions> options,
    ILogger<DatabaseMigrationWorker> logger) : BackgroundService
{
    private readonly DatabaseMigratorHealthcheckOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ResetMarkers();

        try
        {
            logger.LogInformation("Starting database deployment");
            await services.InitializeDatabaseAsync(environment.ContentRootPath, stoppingToken);
            WriteMarker(_options.ReadyFilePath, "ready");
            logger.LogInformation("Database deployment completed successfully. Healthcheck marker created at {ReadyFilePath}", _options.ReadyFilePath);
        }
        catch (Exception exception)
        {
            WriteMarker(_options.FailedFilePath, exception.ToString());
            logger.LogError(exception, "Database deployment failed. Failure marker created at {FailedFilePath}", _options.FailedFilePath);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void ResetMarkers()
    {
        DeleteMarkerIfExists(_options.ReadyFilePath);
        DeleteMarkerIfExists(_options.FailedFilePath);
    }

    private static void DeleteMarkerIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void WriteMarker(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }
}
