using BackendProjectTemplate.Domain.Auditing.Entities;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Reads the administrative audit journal from the read context. Filters, search, ordering and paging run in
/// PostgreSQL; names come from the snapshots stored on each record, so no joins to users or resources occur.
/// </summary>
public sealed class AdministrativeAuditReadModelRepository(AppReadDbContext dbContext)
    : IAdministrativeAuditReadModelRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<AdministrativeAuditListPage> ListAsync(
        AdministrativeAuditListRequest request,
        CancellationToken cancellationToken)
    {
        var audits = ApplyFilter(dbContext.AdministrativeAudits.AsNoTracking(), request.Filter);
        var totalCount = await audits.CountAsync(cancellationToken);
        var rows = await Order(audits)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(audit => new
            {
                audit.Id,
                audit.OccurredAtUtc,
                audit.ActorUserId,
                audit.ActorStakeholderId,
                audit.ActorDisplayName,
                audit.Action,
                audit.TargetType,
                audit.TargetId,
                audit.TargetDisplayName,
                audit.Reason,
                audit.ActorTenantId,
                audit.TargetTenantId,
                audit.CorrelationId,
                audit.FlowId,
                audit.Changes
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdministrativeAuditListItem(
                row.Id,
                row.OccurredAtUtc,
                new AdministrativeAuditActorSummary(row.ActorUserId, row.ActorStakeholderId, row.ActorDisplayName),
                row.Action,
                new AdministrativeAuditTargetSummary(row.TargetType, row.TargetId, row.TargetDisplayName),
                row.Reason,
                row.ActorTenantId,
                row.TargetTenantId,
                row.CorrelationId,
                row.FlowId,
                ParseChanges(row.Changes)))
            .ToList();
        return new AdministrativeAuditListPage(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<AdministrativeAuditExportRow>> ListForExportAsync(
        AdministrativeAuditFilter filter,
        int maxRows,
        CancellationToken cancellationToken) =>
        await Order(ApplyFilter(dbContext.AdministrativeAudits.AsNoTracking(), filter))
            .Take(maxRows)
            .Select(audit => new AdministrativeAuditExportRow(
                audit.Id,
                audit.OccurredAtUtc,
                audit.ActorUserId,
                audit.ActorDisplayName,
                audit.Action,
                audit.TargetType,
                audit.TargetId,
                audit.TargetDisplayName,
                audit.Reason,
                audit.TargetTenantId,
                audit.CorrelationId))
            .ToListAsync(cancellationToken);

    private static IQueryable<AdministrativeAudit> ApplyFilter(
        IQueryable<AdministrativeAudit> audits,
        AdministrativeAuditFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{EscapeLikePattern(filter.Search.Trim())}%";
            audits = audits.Where(audit =>
                EF.Functions.ILike(audit.ActorDisplayName, pattern, LikeEscapeCharacter) ||
                (audit.TargetDisplayName != null && EF.Functions.ILike(audit.TargetDisplayName, pattern, LikeEscapeCharacter)) ||
                (audit.Reason != null && EF.Functions.ILike(audit.Reason, pattern, LikeEscapeCharacter)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            audits = audits.Where(audit => audit.Action == filter.Action);
        }

        if (filter.ActorUserId.HasValue)
        {
            audits = audits.Where(audit => audit.ActorUserId == filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetType))
        {
            audits = audits.Where(audit => audit.TargetType == filter.TargetType);
        }

        if (filter.TargetId.HasValue)
        {
            audits = audits.Where(audit => audit.TargetId == filter.TargetId.Value);
        }

        if (filter.TenantId.HasValue)
        {
            audits = audits.Where(audit => audit.TargetTenantId == filter.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            audits = audits.Where(audit => audit.CorrelationId == filter.CorrelationId);
        }

        if (filter.FromUtc.HasValue)
        {
            var fromUtc = filter.FromUtc.Value.ToUniversalTime();
            audits = audits.Where(audit => audit.OccurredAtUtc >= fromUtc);
        }

        if (filter.ToUtc.HasValue)
        {
            var toUtc = filter.ToUtc.Value.ToUniversalTime();
            audits = audits.Where(audit => audit.OccurredAtUtc < toUtc);
        }

        return audits;
    }

    private static IOrderedQueryable<AdministrativeAudit> Order(IQueryable<AdministrativeAudit> audits) =>
        audits.OrderByDescending(audit => audit.OccurredAtUtc).ThenByDescending(audit => audit.Id);

    /// <summary>Search terms match literally: LIKE wildcards and the escape character are escaped.</summary>
    private static string EscapeLikePattern(string value) =>
        value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

    private static JsonElement? ParseChanges(string? changes)
    {
        if (changes is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(changes);
        return document.RootElement.Clone();
    }
}
