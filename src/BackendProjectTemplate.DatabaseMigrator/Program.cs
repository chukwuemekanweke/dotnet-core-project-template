using BackendProjectTemplate.Infrastructure.DependencyInjection;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrator");

try
{
    logger.LogInformation("Starting database deployment");
    await host.Services.InitializeDatabaseAsync(builder.Environment.ContentRootPath);
    logger.LogInformation("Database deployment completed successfully");
    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Database deployment failed");
    return 1;
}
