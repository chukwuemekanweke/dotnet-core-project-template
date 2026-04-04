using BackendProjectTemplate.DatabaseMigrator.Healthcheck;
using BackendProjectTemplate.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DatabaseMigratorHealthcheckOptions>(
    builder.Configuration.GetSection(DatabaseMigratorHealthcheckOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseMigrationWorker>();

await builder.Build().RunAsync();
