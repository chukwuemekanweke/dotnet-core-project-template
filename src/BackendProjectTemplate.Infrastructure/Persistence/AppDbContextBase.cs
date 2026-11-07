using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.FileUploads.Entities;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Providers.Entities;
using BackendProjectTemplate.Domain.ReferenceData.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public abstract class AppDbContextBase<TContext>(DbContextOptions<TContext> options)
    : IdentityDbContext<
        AppUser,
        IdentityRole<Guid>,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>,
        IdentityUserPasskey<Guid>>(options)
    where TContext : DbContext
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CountryCurrency> CountryCurrencies => Set<CountryCurrency>();
    public DbSet<MessageInbox> MessageInboxes => Set<MessageInbox>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
    public DbSet<PaymentProviderConfiguration> PaymentProviderConfigurations => Set<PaymentProviderConfiguration>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhookInbox> PaymentWebhookInboxes => Set<PaymentWebhookInbox>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<SubscriptionActivation> SubscriptionActivations => Set<SubscriptionActivation>();
    public DbSet<EmailNotificationTemplate> EmailNotificationTemplates => Set<EmailNotificationTemplate>();
    public DbSet<EmailNotificationLog> EmailNotificationLogs => Set<EmailNotificationLog>();
    public DbSet<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxes => Set<EmailDeliveryWebhookInbox>();
    public DbSet<TenantEmailBaseTemplate> TenantEmailBaseTemplates => Set<TenantEmailBaseTemplate>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();
    public DbSet<FileUploadSession> FileUploadSessions => Set<FileUploadSession>();
    public DbSet<StakeholderType> StakeholderTypes => Set<StakeholderType>();
    public DbSet<AuthenticationRefreshToken> AuthenticationRefreshTokens => Set<AuthenticationRefreshToken>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<IpAddress> IpAddresses => Set<IpAddress>();
    public DbSet<IpAddressLocation> IpAddressLocations => Set<IpAddressLocation>();
    public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", SchemaNames.Authentication);
        modelBuilder.Entity<AppUser>().Property(user => user.PhoneNumber).Metadata.SetMaxLength(null);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().Property(login => login.LoginProvider).Metadata.SetMaxLength(null);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().Property(login => login.ProviderKey).Metadata.SetMaxLength(null);
        modelBuilder.Entity<IdentityUserToken<Guid>>().Property(token => token.LoginProvider).Metadata.SetMaxLength(null);
        modelBuilder.Entity<IdentityUserToken<Guid>>().Property(token => token.Name).Metadata.SetMaxLength(null);
        var passkey = modelBuilder.Entity<IdentityUserPasskey<Guid>>();
        passkey.HasKey(value => value.CredentialId);
        passkey.ToTable("UserPasskeys", SchemaNames.Authentication);
        passkey.Property(value => value.CredentialId).HasMaxLength(1024);
        passkey.HasOne<AppUser>().WithMany().HasForeignKey(value => value.UserId).IsRequired();
        if (modelBuilder.Model.FindEntityType(typeof(IdentityPasskeyData)) is not null)
        {
            modelBuilder.Ignore<IdentityPasskeyData>();
            passkey.ComplexProperty(value => value.Data).ToJson();
        }
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", SchemaNames.Authentication);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyAuditingConventions(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplyAuditingConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => type.ClrType is not null && !type.IsOwned()))
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(200);
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(200);
            }

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(ISoftDelete.DeletedBy)).HasMaxLength(200);
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => type.ClrType is not null && !type.IsOwned()))
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeletedProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(ISoftDelete.IsDeleted)));
            var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(compareExpression, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
