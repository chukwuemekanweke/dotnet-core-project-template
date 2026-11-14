using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionActivationConfiguration : IEntityTypeConfiguration<SubscriptionActivation>
{
    public void Configure(EntityTypeBuilder<SubscriptionActivation> builder)
    {
        builder.ToTable("SubscriptionActivations", SchemaNames.Payments);

        builder.HasKey(activation => activation.Id);

        builder.Property(activation => activation.BillingAccountId).IsRequired();

        builder.Property(activation => activation.SubscriptionReference)
            .HasMaxLength(200);

        builder.Property(activation => activation.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(activation => activation.PaymentTransactionId)
            .IsUnique();

        builder.HasIndex(activation => activation.BillingAccountId);

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(activation => activation.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(activation => activation.SubscriptionPeriod)
            .WithMany()
            .HasForeignKey(activation => activation.SubscriptionPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(activation => new { activation.TenantId, activation.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
