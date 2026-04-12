using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public abstract class AppDbContextBase<TContext>(DbContextOptions<TContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
    where TContext : DbContext
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<EmailProvider> EmailProviders => Set<EmailProvider>();
    public DbSet<EmailNotificationTemplate> EmailNotificationTemplates => Set<EmailNotificationTemplate>();
    public DbSet<TenantEmailBaseTemplate> TenantEmailBaseTemplates => Set<TenantEmailBaseTemplate>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();
    public DbSet<StakeholderType> StakeholderTypes => Set<StakeholderType>();
    public DbSet<AppUserStakeholder> AppUserStakeholders => Set<AppUserStakeholder>();

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
