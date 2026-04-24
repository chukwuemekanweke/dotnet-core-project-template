using BackendProjectTemplate.Domain.Providers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers", SchemaNames.Infrastructure);

        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.ProviderType)
            .IsRequired();

        builder.Property(provider => provider.ProviderName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(provider => provider.ProviderKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(provider => provider.IsActive)
            .IsRequired();

        builder.HasIndex(provider => new { provider.ProviderType, provider.ProviderKey })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(provider => new { provider.ProviderType, provider.IsActive })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .IsUnique();
    }
}
