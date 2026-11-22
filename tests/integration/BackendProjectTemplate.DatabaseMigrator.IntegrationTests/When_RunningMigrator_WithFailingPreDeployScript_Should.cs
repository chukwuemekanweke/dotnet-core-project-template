using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithFailingPreDeployScript_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly MigrationDatabase _database = fixture.CreateDatabase();
    private string _contentRoot = string.Empty;

    public Task InitializeAsync()
    {
        _contentRoot = MigratorProcess.CreateContentRoot("PreDeploy", "0000.failing.predeploy.sql", "SELECT 1 / 0;");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DropAsync();
        Directory.Delete(_contentRoot, recursive: true);
    }

    [Fact]
    public async Task ExitWithNonZeroBeforeApplyingEfCoreMigrations()
    {
        // When
        var result = await MigratorProcess.RunAsync(_database.ConnectionString, _contentRoot);

        // Then
        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("PreDeploy script 0000.failing.predeploy.sql failed");
        (await _database.GetAppliedMigrationIdsAsync()).ShouldBeEmpty();
    }
}
