using BackendProjectTemplate.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class UserActivityObservationReceiptConfiguration : IEntityTypeConfiguration<UserActivityObservationReceipt>
{
    public void Configure(EntityTypeBuilder<UserActivityObservationReceipt> builder)
    {
        builder.ToTable("UserActivityObservationReceipts", SchemaNames.Analytics);
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.ActivityType).HasMaxLength(100).IsRequired();
        builder.Property(receipt => receipt.ActivityDate).IsRequired();
        builder.Property(receipt => receipt.OccurredAtUtc).IsRequired();
        builder.HasIndex(receipt => receipt.ObservationId).IsUnique();
        builder.HasIndex(receipt => new { receipt.TenantId, receipt.StakeholderId, receipt.ActivityDate });
    }
}
