using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

public sealed class AdministrativeSecurityAuditConfiguration : IEntityTypeConfiguration<AdministrativeSecurityAudit>
{
    public void Configure(EntityTypeBuilder<AdministrativeSecurityAudit> builder)
    {
        builder.ToTable("AdministrativeSecurityAudits", SchemaNames.Authentication);
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Reason).HasMaxLength(AdministrativeSecurityAudit.MaxReasonLength).IsRequired();
        builder.Property(audit => audit.CorrelationId).HasMaxLength(AdministrativeSecurityAudit.MaxCorrelationLength).IsRequired();
        builder.Property(audit => audit.FlowId).HasMaxLength(AdministrativeSecurityAudit.MaxCorrelationLength);
        builder.Property(audit => audit.PreviousRoles).HasMaxLength(AdministrativeSecurityAudit.MaxRolesLength);
        builder.Property(audit => audit.ResultingRoles).HasMaxLength(AdministrativeSecurityAudit.MaxRolesLength);
        builder.HasIndex(audit => audit.TargetUserId);
        builder.HasIndex(audit => audit.ActorUserId);
        builder.HasIndex(audit => audit.OccurredAtUtc);
    }
}
