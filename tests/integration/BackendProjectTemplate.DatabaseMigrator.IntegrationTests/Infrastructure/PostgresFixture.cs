using Testcontainers.PostgreSql;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        try
        {
            Postgres = new PostgreSqlBuilder("postgres:15.1")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await Postgres.StartAsync();
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw new InvalidOperationException(
                "Docker is unavailable. DatabaseMigrator integration tests require Docker and are never skipped; start Docker and re-run.",
                exception);
        }
    }

    public async Task DisposeAsync()
    {
        if (Postgres is not null)
        {
            await Postgres.DisposeAsync();
        }
    }

    /// <summary>Creates a handle to a uniquely named database the migrator itself will create on first run.</summary>
    public MigrationDatabase CreateDatabase() =>
        new(Postgres.GetConnectionString(), $"migrator_tests_{Guid.CreateVersion7():N}");
}
