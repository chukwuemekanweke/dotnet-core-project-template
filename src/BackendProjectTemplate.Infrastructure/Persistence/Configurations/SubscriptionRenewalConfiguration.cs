using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionRenewalConfiguration : IEntityTypeConfiguration<SubscriptionRenewal>
{
    public void Configure(EntityTypeBuilder<SubscriptionRenewal> builder)
    {
        builder.ToTable("SubscriptionRenewals", SchemaNames.Payments);
        builder.HasKey(renewal => renewal.Id);
        builder.Property(renewal => renewal.ExpectedPrice).HasPrecision(18, 0).IsRequired();
        builder.Property(renewal => renewal.RowVersion).IsRowVersion();
        builder.HasIndex(renewal => new { renewal.SubscriptionId, renewal.TargetPeriodStartUtc }).IsUnique();
        builder.HasIndex(renewal => new { renewal.Status, renewal.NextRetryAtUtc });
        builder.HasOne<Subscription>().WithMany().HasForeignKey(renewal => renewal.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(renewal => renewal.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(renewal => new { renewal.TenantId, renewal.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(renewal => renewal.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(renewal => renewal.Attempts)
            .WithOne()
            .HasForeignKey(attempt => attempt.RenewalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
