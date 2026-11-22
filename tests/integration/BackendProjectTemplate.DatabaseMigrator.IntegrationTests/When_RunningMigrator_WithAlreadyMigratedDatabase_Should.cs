using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithAlreadyMigratedDatabase_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly MigrationDatabase _database = fixture.CreateDatabase();

    public async Task InitializeAsync()
    {
        var firstRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        firstRun.ExitCode.ShouldBe(0, firstRun.Output);
    }

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task ExitWithZeroWithoutDuplicatingSeedData()
    {
        // Given
        var countriesBefore = await _database.CountAsync("""reference_data."Countries" """);
        var plansBefore = await _database.CountAsync("""payments."SubscriptionPlans" """);

        // When: a retried deployment runs the same release again with no pending migrations.
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);

        // Then
        result.ExitCode.ShouldBe(0, result.Output);
        (await _database.GetAppliedMigrationIdsAsync()).ShouldBe(MigrationDatabase.ExpectedMigrationIds);
        (await _database.CountAsync("""reference_data."Countries" """)).ShouldBe(countriesBefore);
        (await _database.CountAsync("""payments."SubscriptionPlans" """)).ShouldBe(plansBefore);
    }
}
