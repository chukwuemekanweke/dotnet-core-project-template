using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class KycWebhookInboxConfiguration : IEntityTypeConfiguration<KycWebhookInbox>
{
    public void Configure(EntityTypeBuilder<KycWebhookInbox> builder)
    {
        builder.ToTable("KycWebhookInboxes", SchemaNames.Kyc);
        builder.HasKey(value => value.Id);
        builder.Property(value => value.ProviderKey).HasMaxLength(50).IsRequired();
        builder.Property(value => value.RequestId).HasMaxLength(100).IsRequired();
        builder.Property(value => value.EventType).HasMaxLength(100).IsRequired();
        builder.Property(value => value.VerificationReference).HasMaxLength(100);
        builder.Property(value => value.SessionReference).HasMaxLength(100);
        builder.Property(value => value.ApplicantReference).HasMaxLength(100);
        builder.Property(value => value.ExternalApplicantId).HasMaxLength(50);
        builder.Property(value => value.ProviderStatus).HasMaxLength(50).IsRequired();
        builder.HasIndex(value => new { value.ProviderKey, value.RequestId }).IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName(KycPersistenceConstraints.WebhookRequestUniqueIndexName);
    }
}
