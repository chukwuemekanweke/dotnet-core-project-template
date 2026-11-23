using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using System.Reflection;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;

public sealed class MigrationDatabase(string serverConnectionString, string databaseName)
{
    public string DatabaseName { get; } = databaseName;

    public string ConnectionString { get; } = new NpgsqlConnectionStringBuilder(serverConnectionString)
    {
        Database = databaseName,
        Pooling = false
    }.ConnectionString;

    public string Password { get; } = new NpgsqlConnectionStringBuilder(serverConnectionString).Password ?? string.Empty;

    public static IReadOnlyCollection<string> ExpectedMigrationIds { get; } = typeof(AppDbContext).Assembly
        .GetTypes()
        .Where(type => type.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(AppDbContext))
        .Select(type => type.GetCustomAttribute<MigrationAttribute>()?.Id)
        .OfType<string>()
        .Order(StringComparer.Ordinal)
        .ToArray();

    public async Task<IReadOnlyCollection<string>> GetAppliedMigrationIdsAsync()
    {
        if (!await TableExistsAsync("public", "__EFMigrationsHistory"))
        {
            return [];
        }

        var ids = new List<string>();
        await using var connection = await OpenAsync(ConnectionString);
        await using var command = new NpgsqlCommand("""SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY 1""", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    public async Task<bool> TableExistsAsync(string schema, string table)
    {
        await using var connection = await OpenAsync(ConnectionString);
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    public async Task<long> CountAsync(string qualifiedTable)
    {
        await using var connection = await OpenAsync(ConnectionString);
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {qualifiedTable}", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = await OpenAsync(ConnectionString);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = await OpenAsync(ConnectionString);
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)value;
    }

    public async Task DropAsync()
    {
        var adminConnectionString = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres" }.ConnectionString;
        await using var connection = await OpenAsync(adminConnectionString);
        await using var command = new NpgsqlCommand(
            $"""DROP DATABASE IF EXISTS "{DatabaseName}" WITH (FORCE)""",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
