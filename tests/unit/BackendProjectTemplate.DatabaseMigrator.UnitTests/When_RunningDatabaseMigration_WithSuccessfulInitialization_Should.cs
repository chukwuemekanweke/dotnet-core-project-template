using BackendProjectTemplate.DatabaseMigrator.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.UnitTests;

public sealed class When_RunningDatabaseMigration_WithSuccessfulInitialization_Should
{
    [Fact]
    public async Task InvokeInitializationOnceAndReturnSuccessExitCode()
    {
        // Given
        var logger = new RecordingLogger<DatabaseMigrationRunner>();
        using var cancellation = new CancellationTokenSource();
        var invocations = 0;
        CancellationToken receivedToken = default;
        var runner = DatabaseMigrationRunnerFactory.Create(
            cancellationToken =>
            {
                invocations++;
                receivedToken = cancellationToken;
                return Task.CompletedTask;
            },
            logger);

        // When
        var exitCode = await runner.RunAsync(cancellation.Token);

        // Then
        exitCode.ShouldBe(DatabaseMigrationRunner.SuccessExitCode);
        exitCode.ShouldBe(0);
        invocations.ShouldBe(1);
        receivedToken.ShouldBe(cancellation.Token);
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Information && entry.Message == "Database migration started");
        logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Information
            && entry.Message.StartsWith("Database migration completed successfully in ", StringComparison.Ordinal));
        logger.Entries.ShouldNotContain(entry => entry.Level >= LogLevel.Error);
    }
}
