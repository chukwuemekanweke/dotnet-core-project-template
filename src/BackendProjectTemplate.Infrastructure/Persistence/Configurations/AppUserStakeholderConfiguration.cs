using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class AppUserStakeholderConfiguration : IEntityTypeConfiguration<AppUserStakeholder>
{
    public void Configure(EntityTypeBuilder<AppUserStakeholder> builder)
    {
        builder.ToTable("AppUserStakeholders", SchemaNames.Stakeholders);
        builder.HasKey(appUserStakeholder => appUserStakeholder.Id);

        builder.HasIndex(appUserStakeholder => new { appUserStakeholder.AppUserId, appUserStakeholder.StakeholderId }).IsUnique();

        builder.HasOne(appUserStakeholder => appUserStakeholder.AppUser)
            .WithMany()
            .HasForeignKey(appUserStakeholder => appUserStakeholder.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(appUserStakeholder => appUserStakeholder.Stakeholder)
            .WithMany(stakeholder => stakeholder.AppUserStakeholders)
            .HasForeignKey(appUserStakeholder => appUserStakeholder.StakeholderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
