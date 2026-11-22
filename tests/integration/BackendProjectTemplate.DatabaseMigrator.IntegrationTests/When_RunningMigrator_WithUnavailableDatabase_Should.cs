using BackendProjectTemplate.DatabaseMigrator.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Sockets;

namespace BackendProjectTemplate.DatabaseMigrator.IntegrationTests;

public sealed class When_RunningMigrator_WithUnavailableDatabase_Should : IAsyncLifetime
{
    private const string Password = "unavailable-database-password";

    private string _connectionString = string.Empty;

    public Task InitializeAsync()
    {
        // Reserve a free local port and release it so nothing is listening there.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _connectionString = $"Host=127.0.0.1;Port={port};Database=unreachable;Username=postgres;Password={Password};Timeout=5";
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExitWithNonZeroWithoutLoggingCredentials()
    {
        // When
        var result = await MigratorProcess.RunAsync(_connectionString);

        // Then
        result.ExitCode.ShouldNotBe(0, result.Output);
        result.Output.ShouldContain("Database initialization stage EnsureDatabase failed");
        result.Output.ShouldContain("Database migration failed");
        result.Output.ShouldNotContain(Password);
    }
}
