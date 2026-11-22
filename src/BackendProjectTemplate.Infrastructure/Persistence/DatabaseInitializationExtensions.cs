using BackendProjectTemplate.Infrastructure.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.RegularExpressions;

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

        // Order is part of the deployment contract: PreDeploy scripts -> EF Core migrations -> authorization
        // seed -> PostDeploy scripts. Each stage commits independently, so a later failure never undoes earlier ones.
        await RunStageAsync(logger, "EnsureDatabase", () => EnsureDatabaseExistsAsync(database.Database.GetConnectionString(), cancellationToken));
        await RunStageAsync(logger, "PreDeploy", () => RunScriptsAsync(database, contentRootPath, "PreDeploy", logger, cancellationToken));
        await RunStageAsync(logger, "EfCoreMigrations", () => database.Database.MigrateAsync(cancellationToken));
        await RunStageAsync(logger, "AuthorizationSeed", () => scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationSeeder>().SeedAsync(cancellationToken));
        await RunStageAsync(logger, "PostDeploy", () => RunScriptsAsync(database, contentRootPath, "PostDeploy", logger, cancellationToken));
    }

    private static async Task RunStageAsync(ILogger logger, string stageName, Func<Task> stage)
    {
        logger.LogInformation("Database initialization stage {StageName} started", stageName);
        try
        {
            await stage();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Logged here so the failing stage is identified; the exception still propagates unchanged.
            logger.LogError("Database initialization stage {StageName} failed", stageName);
            throw;
        }

        logger.LogInformation("Database initialization stage {StageName} completed", stageName);
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
            try
            {
                foreach (var batch in SplitSqlBatches(script))
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    await database.Database.ExecuteSqlRawAsync(batch, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError("{StageName} script {ScriptFile} failed", stageName, Path.GetFileName(scriptFile));
                throw;
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
            throw new InvalidOperationException("A PostgreSQL connection string is required.");
        }

        var targetBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(targetBuilder.Database))
        {
            return;
        }

        var databaseName = targetBuilder.Database;
        var escapedDatabaseName = databaseName.Replace("\"", "\"\"", StringComparison.Ordinal);

        var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT 1
            FROM pg_database
            WHERE datname = @databaseName;
            """;
        command.Parameters.AddWithValue("databaseName", databaseName);

        var exists = await command.ExecuteScalarAsync(cancellationToken) is not null;
        if (exists)
        {
            return;
        }

        await using var createDatabaseCommand = connection.CreateCommand();
        createDatabaseCommand.CommandText = $"""CREATE DATABASE "{escapedDatabaseName}";""";
        await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
