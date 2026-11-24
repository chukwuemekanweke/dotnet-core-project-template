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
        builder.ToTable("SubscriptionPlans", SchemaNames.Payments);

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Code).HasMaxLength(SubscriptionPlan.MaxCodeLength).IsRequired();
        builder.Property(plan => plan.Name).HasMaxLength(SubscriptionPlan.MaxNameLength).IsRequired();
        builder.Property(plan => plan.CurrencyId).IsRequired();
        builder.Property(plan => plan.IsActive).IsRequired();
        builder.Property(plan => plan.Description).HasMaxLength(SubscriptionPlan.MaxDescriptionLength);
        builder.PrimitiveCollection(plan => plan.Features)
            .HasField("_features")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique().HasDatabaseName(SubscriptionPersistenceConstraints.PlanCodeUniqueIndexName);
        builder.HasIndex(plan => plan.IsActive);
        builder.HasOne<Currency>().WithMany().HasForeignKey(plan => plan.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(plan => plan.Prices)
            .WithOne()
            .HasForeignKey(price => price.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(plan => plan.Prices).AutoInclude();
    }
}
