using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> builder)
    {
        builder.ToTable("KycVerifications", SchemaNames.Kyc);
        builder.HasKey(value => value.Id);
        builder.Property(value => value.ProviderKey).HasMaxLength(50).IsRequired();
        builder.Property(value => value.ExternalApplicantId).HasMaxLength(50).IsRequired();
        builder.Property(value => value.ProviderApplicantReference).HasMaxLength(100);
        builder.Property(value => value.ProviderVerificationReference).HasMaxLength(100);
        builder.Property(value => value.ProviderSessionReference).HasMaxLength(100);
        builder.Property(value => value.LastProviderTransition).IsRequired();
        builder.Property(value => value.VerificationUrl).HasMaxLength(2048);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasIndex(value => new { value.TenantId, value.StakeholderId, value.StartedAtUtc });
        builder.HasIndex(value => new { value.ProviderKey, value.ProviderApplicantReference });
        builder.HasIndex(value => new { value.ProviderKey, value.ProviderVerificationReference }).IsUnique()
            .HasFilter("\"ProviderVerificationReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        builder.HasIndex(value => new { value.TenantId, value.StakeholderId }).IsUnique()
            .HasFilter($"\"Status\" IN ({(int)KycVerificationStatus.Pending}, {(int)KycVerificationStatus.InProgress}) AND \"IsDeleted\" = FALSE")
            .HasDatabaseName(KycPersistenceConstraints.ActiveStakeholderUniqueIndexName);
        builder.HasOne<Stakeholder>().WithMany().HasForeignKey(value => new { value.TenantId, value.StakeholderId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
