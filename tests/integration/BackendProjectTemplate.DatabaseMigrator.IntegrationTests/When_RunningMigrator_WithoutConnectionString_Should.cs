using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

public sealed class When_RunningMigrator_WithoutConnectionString_Should : IAsyncLifetime
{
    private string _contentRoot = string.Empty;

    public Task InitializeAsync()
    {
        // A content root without appsettings.json, so no connection string is configured anywhere.
        _contentRoot = MigratorProcess.CreateContentRoot("PreDeploy", "0000.noop.predeploy.sql", "SELECT 1;");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_contentRoot, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExitWithNonZero()
    {
        // When
        var result = await MigratorProcess.RunAsync(connectionString: null, _contentRoot);

        // Then
        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("PostgresWrite");
        result.Output.ShouldContain("Database migration failed");
    }
}
