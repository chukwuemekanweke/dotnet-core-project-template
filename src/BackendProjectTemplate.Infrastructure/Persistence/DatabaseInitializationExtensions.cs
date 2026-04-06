using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default) =>
        app.Services.InitializeDatabaseAsync(app.Environment.ContentRootPath, cancellationToken);

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        string? contentRootPath = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

        await EnsureDatabaseExistsAsync(database.Database.GetConnectionString(), cancellationToken);
        await RunScriptsAsync(database, contentRootPath, "PreDeploy", logger, cancellationToken);
        await database.Database.MigrateAsync(cancellationToken);
        await RunScriptsAsync(database, contentRootPath, "PostDeploy", logger, cancellationToken);
    }

    private static async Task RunScriptsAsync(
        AppDbContext database,
        string? contentRootPath,
        string stageName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath))
        {
            return;
        }

        var scriptsPath = Path.Combine(contentRootPath, "Scripts", stageName);
        if (!Directory.Exists(scriptsPath))
        {
            return;
        }

        var scriptFiles = Directory
            .EnumerateFiles(scriptsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var scriptFile in scriptFiles)
        {
            logger.LogInformation("Executing {StageName} script {ScriptFile}", stageName, Path.GetFileName(scriptFile));

            var script = await File.ReadAllTextAsync(scriptFile, cancellationToken);
            foreach (var batch in SplitSqlBatches(script))
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                await database.Database.ExecuteSqlRawAsync(batch, cancellationToken);
            }
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string script) =>
        Regex.Split(script, @"^\s*GO\s*($|--.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch));

    private static async Task EnsureDatabaseExistsAsync(string? connectionString, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A SQL Server connection string is required.");
        }

        var targetBuilder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(targetBuilder.InitialCatalog))
        {
            return;
        }

        var databaseName = targetBuilder.InitialCatalog;
        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var literalDatabaseName = databaseName.Replace("'", "''", StringComparison.Ordinal);

        var masterBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID(N'{literalDatabaseName}') IS NULL EXEC(N'CREATE DATABASE [{escapedDatabaseName}]')";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
