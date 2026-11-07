using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresWrite")
            ?? throw new InvalidOperationException(
                "Environment variable 'ConnectionStrings__PostgresWrite' is required for design-time AppDbContext creation.");

        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        var serviceProvider = services.BuildServiceProvider();

        optionsBuilder
            .UseNpgsql(PostgresConnectionString.Normalize(connectionString))
            .UseApplicationServiceProvider(serviceProvider);

        return new AppDbContext(optionsBuilder.Options);
    }
}
