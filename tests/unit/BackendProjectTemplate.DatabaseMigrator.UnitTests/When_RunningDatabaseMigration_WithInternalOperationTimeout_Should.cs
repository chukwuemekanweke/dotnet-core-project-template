using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithInternalOperationTimeout_Should
{
    [Fact]
    public async Task TreatTheCancellationAsAFailureAndLogTheException()
    {
        // Given: an operation cancelled internally (e.g. a command timeout) while the host token is not cancelled.
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        var timeout = new TaskCanceledException("Command timed out");
        var runner = DatabaseMigrationRunnerFactory.Create(_ => Task.FromException(timeout), logger);

        // When
        var exitCode = await runner.RunAsync(CancellationToken.None);

        // Then
        exitCode.ShouldBe(DatabaseMigrationRunner.FailureExitCode);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Exception == timeout);
    }
}
