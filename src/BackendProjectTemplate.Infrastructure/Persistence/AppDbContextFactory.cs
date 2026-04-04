using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") ??
            "Server=localhost,14333;Database=BackendProjectTemplateDb;User Id=sa;Password=Your_strong_Password123!;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
