using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class AvatarUploadConfiguration : IEntityTypeConfiguration<AvatarUpload>
{
    public void Configure(EntityTypeBuilder<AvatarUpload> builder)
    {
        builder.ToTable("AvatarUploads", SchemaNames.Stakeholders);
        builder.HasKey(upload => upload.Id);

        builder.Property(upload => upload.StakeholderId).IsRequired();
        builder.Property(upload => upload.TenantId).IsRequired();
        builder.Property(upload => upload.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(upload => upload.ExpectedContentType).HasMaxLength(100).IsRequired();
        builder.Property(upload => upload.ExpectedContentLength).IsRequired();
        builder.Property(upload => upload.FileExtension).HasMaxLength(10).IsRequired();
        builder.Property(upload => upload.QuarantineObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(upload => upload.FinalObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(upload => upload.FinalUrl).HasMaxLength(2048);
        builder.Property(upload => upload.ValidatedETag).HasMaxLength(255);
        builder.Property(upload => upload.Status).IsRequired();
        builder.Property(upload => upload.ExpiresAtUtc).IsRequired();
        builder.Property(upload => upload.RejectionReason).HasMaxLength(100);

        builder.HasIndex(upload => upload.StakeholderId);
        builder.HasIndex(upload => new { upload.Status, upload.ExpiresAtUtc });

        builder.HasOne(upload => upload.Stakeholder)
            .WithMany()
            .HasForeignKey(upload => upload.StakeholderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
