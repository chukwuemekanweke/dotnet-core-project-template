using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Country> Countries => Set<Country>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", SchemaNames.Authentication);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
