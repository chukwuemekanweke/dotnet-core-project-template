using BackendProjectTemplate.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class CountryCurrencyConfiguration : IEntityTypeConfiguration<CountryCurrency>
{
    public void Configure(EntityTypeBuilder<CountryCurrency> builder)
    {
        builder.ToTable("CountryCurrencies", SchemaNames.Payments);

        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.CountryId).IsRequired();
        builder.Property(mapping => mapping.CurrencyId).IsRequired();
        builder.Property(mapping => mapping.IsDefault).IsRequired();
        builder.Property(mapping => mapping.IsActive).IsRequired();

        builder.HasIndex(mapping => new { mapping.CountryId, mapping.CurrencyId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(mapping => new { mapping.CountryId, mapping.IsDefault })
            .HasFilter("[IsDeleted] = 0 AND [IsDefault] = 1")
            .IsUnique();
    }
}
