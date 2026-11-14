using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class RecurringPaymentAuthorizationConfiguration : IEntityTypeConfiguration<RecurringPaymentAuthorization>
{
    public void Configure(EntityTypeBuilder<RecurringPaymentAuthorization> builder)
    {
        builder.ToTable("RecurringPaymentAuthorizations", SchemaNames.Payments);
        builder.HasKey(authorization => authorization.Id);
        builder.Property(authorization => authorization.AuthorizationReference)
            .HasMaxLength(RecurringPaymentAuthorization.MaxReferenceLength)
            .IsRequired(false);
        builder.Property(authorization => authorization.ProviderCustomerReference)
            .HasMaxLength(RecurringPaymentAuthorization.MaxReferenceLength)
            .IsRequired();
        builder.Property(authorization => authorization.RowVersion).IsRowVersion();
        builder.HasIndex(authorization => new
        {
            authorization.BillingAccountId,
            authorization.PaymentProviderId,
            authorization.ProviderCustomerReference
        }).IsUnique();
        builder.HasIndex(authorization => authorization.SourcePaymentTransactionId).IsUnique();
        builder.HasIndex(authorization => new { authorization.TenantId, authorization.BillingAccountId, authorization.Status });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(authorization => authorization.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BillingAccount>()
            .WithMany()
            .HasForeignKey(authorization => new { authorization.TenantId, authorization.BillingAccountId })
            .HasPrincipalKey(account => new { account.TenantId, account.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentProvider>().WithMany().HasForeignKey(authorization => authorization.PaymentProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(authorization => authorization.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(authorization => authorization.SourcePaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
