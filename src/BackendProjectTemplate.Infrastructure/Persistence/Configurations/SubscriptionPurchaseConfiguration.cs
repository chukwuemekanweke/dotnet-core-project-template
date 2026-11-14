using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPurchaseConfiguration : IEntityTypeConfiguration<SubscriptionPurchase>
{
    public void Configure(EntityTypeBuilder<SubscriptionPurchase> builder)
    {
        builder.ToTable(
            "SubscriptionPurchases",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPurchases_ExpectedAmount",
                    "\"ExpectedAmount\" > 0 AND \"ExpectedAmount\" = trunc(\"ExpectedAmount\")");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPurchases_BillingInterval",
                    "\"BillingInterval\" IN (1, 2)");
            });
        builder.HasKey(purchase => purchase.Id);
        builder.Property(purchase => purchase.PlanCode).HasMaxLength(SubscriptionPurchase.MaxPlanCodeLength).IsRequired();
        builder.Property(purchase => purchase.ExpectedAmount).HasPrecision(18, 0).IsRequired();
        builder.HasIndex(purchase => purchase.PaymentTransactionId).IsUnique();
        builder.HasIndex(purchase => purchase.SubscriptionId).IsUnique();
        builder.HasIndex(purchase => new { purchase.TenantId, purchase.BillingAccountId });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(purchase => purchase.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(purchase => new { purchase.TenantId, purchase.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Subscription>().WithMany().HasForeignKey(purchase => purchase.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(purchase => purchase.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(purchase => purchase.ExpectedCurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentTransaction>().WithMany().HasForeignKey(purchase => purchase.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(purchase => new { purchase.TenantId, purchase.InitiatedByStakeholderId })
            .HasPrincipalKey(stakeholder => new { stakeholder.TenantId, stakeholder.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
