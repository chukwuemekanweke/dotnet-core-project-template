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

    [Fact]
    public async Task SeedThreePackagesEachWithMonthlyAndAnnualPricesAndCardContent()
    {
        // When
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);

        // Then
        result.ExitCode.ShouldBe(0, result.Output);
        var packages = await _database.ScalarAsync<string>("""
            SELECT string_agg(p."Code" || ':' || m."Price" || ':' || y."Price" || ':' || cardinality(p."Features"), ',' ORDER BY m."Price")
            FROM payments."SubscriptionPlans" p
            INNER JOIN payments."SubscriptionPlanPrices" m ON m."PlanId" = p."Id" AND m."BillingInterval" = 1
            INNER JOIN payments."SubscriptionPlanPrices" y ON y."PlanId" = p."Id" AND y."BillingInterval" = 2
            WHERE p."IsActive" AND p."Description" IS NOT NULL
            """);
        packages.ShouldBe("BASIC:500000:5000000:3,STANDARD:1500000:15000000:4,PREMIUM:4000000:40000000:4");
        (await _database.ScalarAsync<long>("""
            SELECT COUNT(*) FROM payments."SubscriptionPlans" WHERE "Code" IN ('STARTER_MONTHLY', 'STARTER_YEARLY')
            """)).ShouldBe(0);
    }
}
