using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithLongRunningInitialization_Should
{
    [Fact]
    public async Task CompleteAsSoonAsInitializationCompletes()
    {
        // Given
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        var initialization = new TaskCompletionSource();
        var runner = DatabaseMigrationRunnerFactory.Create(_ => initialization.Task, logger);

        // When
        var run = runner.RunAsync(CancellationToken.None);
        run.IsCompleted.ShouldBeFalse();
        initialization.SetResult();

        // Then: no keep-alive loop remains once the work finishes.
        var exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));
        exitCode.ShouldBe(DatabaseMigrationRunner.SuccessExitCode);
    }
}
