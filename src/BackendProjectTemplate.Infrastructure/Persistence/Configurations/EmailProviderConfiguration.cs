using BackendProjectTemplate.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class EmailProviderConfiguration : IEntityTypeConfiguration<EmailProvider>
{
    public void Configure(EntityTypeBuilder<EmailProvider> builder)
    {
        builder.ToTable("EmailProviders", SchemaNames.Notifications);

        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.ProviderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(provider => provider.ProviderKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(provider => provider.IsActive)
            .IsRequired();

        builder.HasIndex(provider => provider.ProviderKey)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(provider => provider.IsActive)
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .IsUnique();
    }
}
