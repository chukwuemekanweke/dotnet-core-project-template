using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithAlreadyCancelledToken_Should
{
    [Fact]
    public async Task NotInitializeTheDatabaseAndReturnFailureExitCode()
    {
        // Given
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        var invocations = 0;
        var runner = DatabaseMigrationRunnerFactory.Create(
            _ =>
            {
                invocations++;
                return Task.CompletedTask;
            },
            logger);

        // When
        var exitCode = await runner.RunAsync(new CancellationToken(canceled: true));

        // Then
        exitCode.ShouldBe(DatabaseMigrationRunner.FailureExitCode);
        invocations.ShouldBe(0);
    }
}
