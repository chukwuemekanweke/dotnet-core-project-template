using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class SecurityActivityConfiguration : IEntityTypeConfiguration<SecurityActivity>
{
    public void Configure(EntityTypeBuilder<SecurityActivity> builder)
    {
        builder.ToTable("LoginActivities", SchemaNames.Authentication);
        builder.Property(securityActivity => securityActivity.UserAgent).HasMaxLength(500);
        builder.Property(securityActivity => securityActivity.DeviceName).HasMaxLength(200);
        builder.Property(securityActivity => securityActivity.DevicePlatform).HasMaxLength(100);
        builder.Property(securityActivity => securityActivity.BrowserName).HasMaxLength(100);
        builder.Property(securityActivity => securityActivity.FailureReason).HasMaxLength(SecurityActivity.MaxFailureReasonLength);

        builder
            .HasOne(securityActivity => securityActivity.IpAddress)
            .WithMany()
            .HasForeignKey(securityActivity => securityActivity.IpAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(securityActivity => securityActivity.IpAddressLocation)
            .WithMany()
            .HasForeignKey(securityActivity => securityActivity.IpAddressLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(securityActivity => securityActivity.StakeholderId);
        builder.HasIndex(securityActivity => securityActivity.TenantId);
        builder.HasIndex(securityActivity => securityActivity.OccurredAtUtc);
        builder.HasIndex(securityActivity => securityActivity.IpAddressId);
        builder.HasIndex(securityActivity => securityActivity.IpAddressLocationId);
        builder.HasIndex(securityActivity => new
        {
            securityActivity.TenantId,
            securityActivity.StakeholderId,
            securityActivity.OccurredAtUtc,
            securityActivity.Id
        });
        builder.HasIndex(securityActivity => new
        {
            securityActivity.TenantId,
            securityActivity.StakeholderId,
            securityActivity.ActivityType,
            securityActivity.OccurredAtUtc
        }).IsDescending(false, false, false, true);
    }
}
