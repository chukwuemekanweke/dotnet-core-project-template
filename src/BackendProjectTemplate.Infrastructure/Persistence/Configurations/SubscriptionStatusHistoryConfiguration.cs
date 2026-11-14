using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionStatusHistoryConfiguration : IEntityTypeConfiguration<SubscriptionStatusHistory>
{
    public void Configure(EntityTypeBuilder<SubscriptionStatusHistory> builder)
    {
        builder.ToTable(
            "SubscriptionStatusHistory",
            SchemaNames.Payments,
            tableBuilder =>
            {
                const string validStatuses = "1, 2, 3, 4";
                tableBuilder.HasCheckConstraint("CK_SubscriptionStatusHistory_NewStatus", $"\"NewStatus\" IN ({validStatuses})");
                tableBuilder.HasCheckConstraint(
                    "CK_SubscriptionStatusHistory_PreviousStatus",
                    $"\"PreviousStatus\" IS NULL OR \"PreviousStatus\" IN ({validStatuses})");
            });
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Reason).HasMaxLength(SubscriptionStatusHistory.MaxReasonLength).IsRequired();
        builder.Property(history => history.ActorId).HasMaxLength(SubscriptionStatusHistory.MaxActorIdLength);
        builder.HasIndex(history => new { history.SubscriptionId, history.OccurredAtUtc });
        builder.HasIndex(history => new { history.SubscriptionId, history.OperationId }).IsUnique();
        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(history => history.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
