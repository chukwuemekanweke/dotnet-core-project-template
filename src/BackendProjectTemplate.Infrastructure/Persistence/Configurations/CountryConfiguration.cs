using BackendProjectTemplate.Domain.ReferenceData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries", SchemaNames.ReferenceData);
        builder.HasKey(country => country.Id);

        builder.Property(country => country.Code).HasMaxLength(3).IsRequired();
        builder.Property(country => country.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(country => country.Code).IsUnique();
    }
}
