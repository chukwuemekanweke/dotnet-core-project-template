using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanPriceConfiguration : IEntityTypeConfiguration<SubscriptionPlanPrice>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanPrice> builder)
    {
        builder.ToTable(
            "SubscriptionPlanPrices",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_SubscriptionPlanPrices_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionPlanPrices_BillingInterval",
                    $"\"BillingInterval\" IN ({(int)BillingInterval.Monthly}, {(int)BillingInterval.Yearly})");
            });

        builder.HasKey(price => price.Id);
        builder.Property(price => price.PlanId).IsRequired();
        builder.Property(price => price.BillingInterval).IsRequired();
        builder.Property(price => price.Price).HasPrecision(18, 0).IsRequired();
        builder.HasIndex(price => new { price.PlanId, price.BillingInterval }).IsUnique();
    }
}
