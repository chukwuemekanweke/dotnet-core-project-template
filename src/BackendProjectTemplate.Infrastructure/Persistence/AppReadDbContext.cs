using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AppReadDbContext(DbContextOptions<AppReadDbContext> options)
    : AppDbContextBase<AppReadDbContext>(options);
