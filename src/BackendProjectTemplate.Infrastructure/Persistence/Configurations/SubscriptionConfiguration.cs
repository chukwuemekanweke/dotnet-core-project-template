using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable(
            "Subscriptions",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Subscriptions_Status",
                    $"\"Status\" IN ({(int)SubscriptionStatus.Pending}, {(int)SubscriptionStatus.Active}, {(int)SubscriptionStatus.Cancelled}, {(int)SubscriptionStatus.Expired}, {(int)SubscriptionStatus.Scheduled})");
                tableBuilder.HasCheckConstraint(
                    "CK_Subscriptions_EntitlementWindow",
                    $"(\"Status\" NOT IN ({(int)SubscriptionStatus.Active}, {(int)SubscriptionStatus.Scheduled}) OR \"EntitlementStartUtc\" IS NOT NULL) "
                    + "AND (\"EntitlementStartUtc\" IS NULL OR \"EntitlementEndUtc\" IS NULL OR \"EntitlementEndUtc\" >= \"EntitlementStartUtc\")");
                tableBuilder.HasCheckConstraint(
                    "CK_Subscriptions_CurrentPeriod",
                    "\"CurrentPeriodStartUtc\" IS NULL OR \"CurrentPeriodEndUtc\" > \"CurrentPeriodStartUtc\"");
                tableBuilder.HasCheckConstraint(
                    "CK_Subscriptions_BillingInterval",
                    $"\"BillingInterval\" IN ({(int)BillingInterval.Monthly}, {(int)BillingInterval.Yearly})");
            });

        builder.HasKey(subscription => subscription.Id);
        builder.HasAlternateKey(subscription => new { subscription.TenantId, subscription.Id });
        builder.Property(subscription => subscription.TenantId).IsRequired();
        builder.Property(subscription => subscription.BillingAccountId).IsRequired();
        builder.Property(subscription => subscription.PlanId).IsRequired();
        builder.Property(subscription => subscription.BillingInterval).IsRequired();
        builder.Property(subscription => subscription.Status).IsRequired();
        builder.Property(subscription => subscription.CancellationReason).HasMaxLength(Subscription.MaxCancellationReasonLength);
        builder.Property(subscription => subscription.RowVersion).IsRowVersion();
        builder.HasIndex(subscription => new { subscription.TenantId, subscription.BillingAccountId, subscription.Status });
        builder.HasIndex(subscription => new { subscription.Status, subscription.NextBillingAtUtc });
        builder.HasIndex(subscription => new { subscription.CreatedAtUtc, subscription.Id });
        builder.HasIndex(subscription => subscription.PlanId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(subscription => subscription.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(subscription => new { subscription.TenantId, subscription.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(subscription => subscription.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(subscription => subscription.LastSuccessfulPaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(subscription => subscription.Periods)
            .WithOne()
            .HasForeignKey(period => period.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(subscription => subscription.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
