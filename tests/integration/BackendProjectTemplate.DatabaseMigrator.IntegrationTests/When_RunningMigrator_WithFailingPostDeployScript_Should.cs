using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithFailingPostDeployScript_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly MigrationDatabase _database = fixture.CreateDatabase();
    private string _contentRoot = string.Empty;

    public Task InitializeAsync()
    {
        _contentRoot = MigratorProcess.CreateContentRoot("PostDeploy", "9999.failing.postdeploy.sql", "SELECT 1 / 0;");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _database.DropAsync();
        Directory.Delete(_contentRoot, recursive: true);
    }

    [Fact]
    public async Task ExitWithNonZeroAndKeepAlreadyCommittedMigrations()
    {
        // When
        var result = await MigratorProcess.RunAsync(_database.ConnectionString, _contentRoot);

        // Then: the failure is reported, and earlier stages stay committed (no automatic reverse migration).
        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("PostDeploy script 9999.failing.postdeploy.sql failed");
        (await _database.GetAppliedMigrationIdsAsync()).ShouldBe(MigrationDatabase.ExpectedMigrationIds);
    }
}
