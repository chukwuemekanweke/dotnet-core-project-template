using BackendProjectTemplate.Domain.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendProjectTemplate.Infrastructure.Persistence.Configurations;

/// <summary>
/// Append-only administrative audit journal. Actor, target and tenant identifiers are deliberately not foreign
/// keys, so audit retention is independent of the lifecycle of the users, tenants and resources they reference.
/// </summary>
public sealed class AdministrativeAuditConfiguration : IEntityTypeConfiguration<AdministrativeAudit>
{
    public void Configure(EntityTypeBuilder<AdministrativeAudit> builder)
    {
        builder.ToTable("AdministrativeAudits", SchemaNames.Auditing);
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.ActorDisplayName).HasMaxLength(AdministrativeAudit.MaxDisplayNameLength).IsRequired();
        builder.Property(audit => audit.Action).HasMaxLength(AdministrativeAudit.MaxActionLength).IsRequired();
        builder.Property(audit => audit.Reason).HasMaxLength(AdministrativeAudit.MaxReasonLength);
        builder.Property(audit => audit.TargetType).HasMaxLength(AdministrativeAudit.MaxTargetTypeLength).IsRequired();
        builder.Property(audit => audit.TargetDisplayName).HasMaxLength(AdministrativeAudit.MaxDisplayNameLength);
        builder.Property(audit => audit.CorrelationId).HasMaxLength(AdministrativeAudit.MaxCorrelationLength).IsRequired();
        builder.Property(audit => audit.FlowId).HasMaxLength(AdministrativeAudit.MaxCorrelationLength);
        builder.Property(audit => audit.Changes).HasColumnType("jsonb");

        builder.HasIndex(audit => new { audit.OccurredAtUtc, audit.Id });
        builder.HasIndex(audit => new { audit.ActorUserId, audit.OccurredAtUtc });
        builder.HasIndex(audit => new { audit.Action, audit.OccurredAtUtc });
        builder.HasIndex(audit => new { audit.TargetType, audit.TargetId, audit.OccurredAtUtc });
        builder.HasIndex(audit => new { audit.TargetTenantId, audit.OccurredAtUtc });
        builder.HasIndex(audit => audit.CorrelationId);
    }
}
