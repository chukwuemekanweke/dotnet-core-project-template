using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithFreshDatabase_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly MigrationDatabase _database = fixture.CreateDatabase();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task CreateMigrateSeedAndExitWithZero()
    {
        // When
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);

        // Then
        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Database migration completed successfully");
        result.Output.ShouldNotContain($"Password={_database.Password}", Case.Insensitive);
        (await _database.GetAppliedMigrationIdsAsync()).ShouldBe(MigrationDatabase.ExpectedMigrationIds);
        (await _database.CountAsync("""authentication."Roles" """)).ShouldBeGreaterThan(0);
        (await _database.CountAsync("""reference_data."Countries" """)).ShouldBeGreaterThan(0);
        (await _database.CountAsync("""payments."SubscriptionPlans" """)).ShouldBeGreaterThan(0);
    }
}
