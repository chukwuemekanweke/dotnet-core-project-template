using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionAllocationConfiguration : IEntityTypeConfiguration<SubscriptionAllocation>
{
    public void Configure(EntityTypeBuilder<SubscriptionAllocation> builder)
    {
        builder.ToTable(
            "SubscriptionAllocations",
            SchemaNames.Payments,
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_SubscriptionAllocations_Window", "\"EndAtUtc\" > \"StartAtUtc\"");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionAllocations_NominalPrice",
                    "\"NominalPrice\" >= 0 AND \"NominalPrice\" = trunc(\"NominalPrice\")");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionAllocations_BillingInterval",
                    $"\"BillingInterval\" IN ({(int)BillingInterval.Monthly}, {(int)BillingInterval.Yearly})");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionAllocations_AllocationSource",
                    $"\"AllocationSource\" IN ({string.Join(", ", Enum.GetValues<SubscriptionAllocationSource>().Select(source => (int)source))})");
            });

        builder.HasKey(allocation => allocation.Id);
        builder.Property(allocation => allocation.PlanCode).HasMaxLength(SubscriptionPlan.MaxCodeLength).IsRequired();
        builder.Property(allocation => allocation.NominalPrice).HasPrecision(18, 0).IsRequired();
        builder.Property(allocation => allocation.Reason).HasMaxLength(SubscriptionAllocation.MaxReasonLength).IsRequired();
        builder.Property(allocation => allocation.AllocatedByActorId).HasMaxLength(SubscriptionAllocation.MaxActorIdLength).IsRequired();
        builder.Property(allocation => allocation.AllocationSource).IsRequired();
        builder.Property(allocation => allocation.RequestFingerprint)
            .HasMaxLength(SubscriptionAllocation.MaxRequestFingerprintLength)
            .IsRequired();
        builder.HasIndex(allocation => allocation.OperationId)
            .IsUnique()
            .HasDatabaseName(SubscriptionPersistenceConstraints.AllocationOperationUniqueIndexName);
        builder.HasIndex(allocation => allocation.SubscriptionId).IsUnique();
        builder.HasIndex(allocation => new { allocation.TenantId, allocation.BillingAccountId });
        builder.HasIndex(allocation => allocation.AllocationSource);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(allocation => allocation.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(allocation => new { allocation.TenantId, allocation.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(allocation => new { allocation.TenantId, allocation.SubscriptionId })
            .HasPrincipalKey(subscription => new { subscription.TenantId, subscription.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(allocation => allocation.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(allocation => allocation.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
