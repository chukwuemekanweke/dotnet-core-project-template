using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class When_RunningMigrator_WithLegacyActiveStorageProvider_Should(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly MigrationDatabase _database = fixture.CreateDatabase();

    public async Task InitializeAsync()
    {
        var firstRun = await MigratorProcess.RunAsync(_database.ConnectionString);
        firstRun.ExitCode.ShouldBe(0, firstRun.Output);

        // Recreate the state seeded before Cloudflare R2 became the active storage provider:
        // 'noop' active and no 'cloudflare_r2' row.
        await _database.ExecuteAsync("""
            DELETE FROM infrastructure."Providers" WHERE "ProviderType" = 2 AND "ProviderKey" = 'cloudflare_r2';
            UPDATE infrastructure."Providers" SET "IsActive" = TRUE WHERE "ProviderType" = 2 AND "ProviderKey" = 'noop';
            """);
    }

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task ActivateCloudflareR2WithoutViolatingTheSingleActiveProviderIndex()
    {
        // When
        var result = await MigratorProcess.RunAsync(_database.ConnectionString);

        // Then
        result.ExitCode.ShouldBe(0, result.Output);
        var activeStorageProvider = await _database.ScalarAsync<string>("""
            SELECT string_agg("ProviderKey", ',') FROM infrastructure."Providers"
            WHERE "ProviderType" = 2 AND "IsActive" = TRUE AND "IsDeleted" = FALSE
            """);
        activeStorageProvider.ShouldBe("cloudflare_r2");
    }
}
