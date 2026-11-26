namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

/// <summary>Query filters shared by the administrative audit list and CSV export.</summary>
public interface IAdministrativeAuditFilterRequest
{
    string? Search { get; }
    string? Action { get; }
    Guid? ActorUserId { get; }
    string? TargetType { get; }
    Guid? TargetId { get; }
    Guid? TenantId { get; }
    string? CorrelationId { get; }
    DateTimeOffset? FromUtc { get; }
    DateTimeOffset? ToUtc { get; }
}
