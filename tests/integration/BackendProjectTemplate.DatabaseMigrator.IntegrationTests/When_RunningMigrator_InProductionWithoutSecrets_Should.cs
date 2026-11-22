using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

public sealed class When_RunningMigrator_InProductionWithoutSecrets_Should : IAsyncLifetime
{
    private const string SecretsDirectory = "/run/secrets/app";

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExitWithNonZeroBeforeTouchingTheDatabase()
    {
        Directory.Exists(SecretsDirectory).ShouldBeFalse(
            $"This test proves the migrator fails without delivered secrets and cannot run if '{SecretsDirectory}' exists.");

        // When: an environment-supplied connection string must never satisfy the production secret-file contract.
        var result = await MigratorProcess.RunAsync(
            "Host=127.0.0.1;Port=1;Database=never;Username=postgres;Password=environment-only-password",
            environment: "Production");

        // Then
        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("Production configuration validation failed for 'BackendProjectTemplate.DatabaseMigrator'");
        result.Output.ShouldNotContain("Database migration started");
        result.Output.ShouldNotContain("environment-only-password");
    }
}
