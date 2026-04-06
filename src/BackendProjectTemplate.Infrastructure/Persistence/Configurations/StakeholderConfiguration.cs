using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("Stakeholders", SchemaNames.Stakeholders);
        builder.HasKey(stakeholder => stakeholder.Id);

        builder.Property(stakeholder => stakeholder.TenantId).IsRequired();
        builder.Property(stakeholder => stakeholder.CountryId).IsRequired();
        builder.Property(stakeholder => stakeholder.StakeholderTypeId).IsRequired();

        builder.HasIndex(stakeholder => stakeholder.TenantId);
        builder.HasIndex(stakeholder => stakeholder.CountryId);
        builder.HasIndex(stakeholder => stakeholder.StakeholderTypeId);

        builder.HasOne(stakeholder => stakeholder.StakeholderType)
            .WithMany(stakeholderType => stakeholderType.Stakeholders)
            .HasForeignKey(stakeholder => stakeholder.StakeholderTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
