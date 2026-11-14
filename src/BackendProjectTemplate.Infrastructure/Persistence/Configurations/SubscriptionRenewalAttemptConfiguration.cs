using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionRenewalAttemptConfiguration : IEntityTypeConfiguration<SubscriptionRenewalAttempt>
{
    public void Configure(EntityTypeBuilder<SubscriptionRenewalAttempt> builder)
    {
        builder.ToTable("SubscriptionRenewalAttempts", SchemaNames.Payments);
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.ProviderIdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(attempt => attempt.FailureCode).HasMaxLength(100);
        builder.Property(attempt => attempt.FailureReason).HasMaxLength(500);
        builder.HasIndex(attempt => new { attempt.RenewalId, attempt.AttemptNumber }).IsUnique();
        builder.HasIndex(attempt => attempt.ProviderIdempotencyKey).IsUnique();
        builder.HasIndex(attempt => attempt.PaymentTransactionId).IsUnique().HasFilter("\"PaymentTransactionId\" IS NOT NULL");
        builder.HasOne<PaymentTransaction>().WithMany().HasForeignKey(attempt => attempt.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringPaymentAuthorization>()
            .WithMany()
            .HasForeignKey(attempt => attempt.RecurringAuthorizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
