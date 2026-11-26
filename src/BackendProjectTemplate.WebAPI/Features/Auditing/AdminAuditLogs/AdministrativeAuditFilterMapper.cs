using BackendProjectTemplate.Domain.Auditing.ReadModels;

namespace BackendProjectTemplate.WebAPI.Features.Auditing.AdminAuditLogs;

internal static class AdministrativeAuditFilterMapper
{
    public static AdministrativeAuditFilter ToFilter(IAdministrativeAuditFilterRequest request) =>
        new(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.Action,
            request.ActorUserId,
            request.TargetType,
            request.TargetId,
            request.TenantId,
            string.IsNullOrWhiteSpace(request.CorrelationId) ? null : request.CorrelationId.Trim(),
            request.FromUtc,
            request.ToUtc);
}
