using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithCancellationDuringInitialization_Should
{
    [Fact]
    public async Task ReturnFailureExitCodeInsteadOfSuccess()
    {
        // Given
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        using var cancellation = new CancellationTokenSource();
        var runner = DatabaseMigrationRunnerFactory.Create(
            async cancellationToken =>
            {
                await cancellation.CancelAsync();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            },
            logger);

        // When
        var exitCode = await runner.RunAsync(cancellation.Token);

        // Then
        exitCode.ShouldBe(DatabaseMigrationRunner.FailureExitCode);
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.StartsWith("Database migration was cancelled after ", StringComparison.Ordinal));
        logger.Entries.ShouldNotContain(entry => entry.Message.StartsWith("Database migration completed", StringComparison.Ordinal));
    }
}
