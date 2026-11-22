using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithFailingInitialization_Should
{
    [Fact]
    public async Task ReturnFailureExitCodeAndLogTheException()
    {
        // Given
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        var failure = new InvalidOperationException("PostDeploy script failed");
        var runner = DatabaseMigrationRunnerFactory.Create(_ => Task.FromException(failure), logger);

        // When
        var exitCode = await runner.RunAsync(CancellationToken.None);

        // Then
        exitCode.ShouldBe(DatabaseMigrationRunner.FailureExitCode);
        exitCode.ShouldNotBe(0);
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Error
            && entry.Exception == failure
            && entry.Message.StartsWith("Database migration failed after ", StringComparison.Ordinal));
        logger.Entries.ShouldNotContain(entry => entry.Message.StartsWith("Database migration completed", StringComparison.Ordinal));
    }
}
