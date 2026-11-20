using BackendProjectTemplate.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class UserDailyActivityConfiguration : IEntityTypeConfiguration<UserDailyActivity>
{
    public void Configure(EntityTypeBuilder<UserDailyActivity> builder)
    {
        builder.ToTable("UserDailyActivities", SchemaNames.Analytics);
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.ActivityDate).IsRequired();
        builder.Property(activity => activity.FirstActivityAtUtc).IsRequired();
        builder.Property(activity => activity.LastActivityAtUtc).IsRequired();
        builder.Property(activity => activity.ActivityCount).IsRequired();
        builder.HasIndex(activity => new { activity.TenantId, activity.StakeholderId, activity.ActivityDate })
            .IsUnique();
        builder.HasIndex(activity => new { activity.TenantId, activity.ActivityDate, activity.StakeholderId });
    }
}
