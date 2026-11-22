namespace BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;

public static class DatabaseMigrationRunnerFactory
{
    public static DatabaseMigrationRunner Create(
        Func<CancellationToken, Task> initializeDatabase,
        RecordingLogger<DatabaseMigrationRunner> logger) =>
        new(initializeDatabase, TimeProvider.System, logger);
}
