using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable(
            "SubscriptionPlans",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_SubscriptionPlans_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPlans_BillingInterval",
                    $"\"BillingInterval\" IN ({(int)BillingInterval.Monthly}, {(int)BillingInterval.Yearly})");
            });

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Code).HasMaxLength(SubscriptionPlan.MaxCodeLength).IsRequired();
        builder.Property(plan => plan.Name).HasMaxLength(SubscriptionPlan.MaxNameLength).IsRequired();
        builder.Property(plan => plan.Price).HasPrecision(18, 0).IsRequired();
        builder.Property(plan => plan.CurrencyId).IsRequired();
        builder.Property(plan => plan.BillingInterval).IsRequired();
        builder.Property(plan => plan.IsActive).IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique();
        builder.HasIndex(plan => plan.IsActive);
        builder.HasOne<Currency>().WithMany().HasForeignKey(plan => plan.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
