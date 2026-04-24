using BackendProjectTemplate.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderConfigurationConfiguration : IEntityTypeConfiguration<PaymentProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<PaymentProviderConfiguration> builder)
    {
        builder.ToTable("PaymentProviderConfigurations", SchemaNames.Payments);

        builder.HasKey(configuration => configuration.Id);

        builder.Property(configuration => configuration.PaymentIntent)
            .IsRequired();

        builder.Property(configuration => configuration.PaymentMethodType)
            .IsRequired();

        builder.Property(configuration => configuration.IsEnabled)
            .IsRequired();

        builder.HasIndex(configuration => new
            {
                configuration.PaymentProviderId,
                configuration.CurrencyId,
                configuration.PaymentIntent
            })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
