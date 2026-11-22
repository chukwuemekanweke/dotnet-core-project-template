using BackendProjectTemplate.DatabaseMigrator;
using BackendProjectTemplate.Infrastructure.Authentication;
using BackendProjectTemplate.Infrastructure.Configuration;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// One-shot executable: run the database initialization pipeline once, then exit with 0 on success or a nonzero
// code on any failure or cancellation. Configuration and production-secret failures throw before the host is
// built, which also terminates the process with a nonzero exit code.
var builder = Host.CreateApplicationBuilder(args);
builder.AddProductionSecrets(
    "BackendProjectTemplate.DatabaseMigrator",
    ProductionSecrets.GetRequirements());

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddSingleton(serviceProvider => new DatabaseMigrationRunner(
    cancellationToken => serviceProvider.InitializeDatabaseAsync(
        serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath,
        cancellationToken),
    serviceProvider.GetRequiredService<TimeProvider>(),
    serviceProvider.GetRequiredService<ILogger<DatabaseMigrationRunner>>()));

var host = builder.Build();
try
{
    // Starting the host runs the production configuration precedence validator and installs the console
    // lifetime, so SIGTERM/Ctrl+C cancel ApplicationStopping instead of killing the migration mid-statement.
    await host.StartAsync();
    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    var exitCode = await host.Services
        .GetRequiredService<DatabaseMigrationRunner>()
        .RunAsync(lifetime.ApplicationStopping);
    await host.StopAsync();
    return exitCode;
}
finally
{
    if (host is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
    }
    else
    {
        host.Dispose();
    }
}
