using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPeriodConfiguration : IEntityTypeConfiguration<SubscriptionPeriod>
{
    public void Configure(EntityTypeBuilder<SubscriptionPeriod> builder)
    {
        builder.ToTable(
            "SubscriptionPeriods",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_SubscriptionPeriods_Boundaries", "\"PeriodEndUtc\" > \"PeriodStartUtc\"");
                tableBuilder.HasCheckConstraint("CK_SubscriptionPeriods_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPeriods_BillingInterval",
                    $"\"BillingInterval\" IN ({(int)BillingInterval.Monthly}, {(int)BillingInterval.Yearly})");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPeriods_ActivationSource",
                    $"\"ActivationSource\" IN ({(int)SubscriptionActivationSource.PaidPurchase}, {(int)SubscriptionActivationSource.AdministrativeAllocation})");
            });

        builder.HasKey(period => period.Id);
        builder.Property(period => period.PlanCode).HasMaxLength(SubscriptionPlan.MaxCodeLength).IsRequired();
        builder.Property(period => period.Price).HasPrecision(18, 0).IsRequired();
        builder.Property(period => period.ActivationSource).IsRequired();
        builder.HasIndex(period => new { period.SubscriptionId, period.PeriodStartUtc });
        builder.HasIndex(period => period.PaymentTransactionId)
            .IsUnique()
            .HasFilter("\"PaymentTransactionId\" IS NOT NULL");
        builder.HasIndex(period => new { period.SubscriptionId, period.OperationId }).IsUnique();
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(period => period.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(period => period.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(period => period.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
