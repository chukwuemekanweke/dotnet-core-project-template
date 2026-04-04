using BackendProjectTemplate.Domain.ReferenceData.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await database.Database.EnsureCreatedAsync();

        if (!await database.Countries.AnyAsync())
        {
            var now = timeProvider.GetUtcNow();

            database.Countries.AddRange(
                Country.Create("NG", "Nigeria", now),
                Country.Create("GB", "United Kingdom", now),
                Country.Create("US", "United States", now));

            await database.SaveChangesAsync();
        }
    }
}
