using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Resolves the acting administrator from server-side context and stages the audit record on the scoped write
/// <see cref="AppDbContext"/>. It never calls SaveChanges: the record commits, or rolls back, with the
/// administrative mutation that staged it.
/// </summary>
internal sealed class AdministrativeAuditRecorder(
    AppDbContext dbContext,
    TimeProvider timeProvider) : IAdministrativeAuditRecorder
{
    public async Task RecordAsync(AdministrativeAuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var actor = await ResolveActorAsync(entry, cancellationToken);
        var audit = AdministrativeAudit.Create(
            actor,
            entry.Action,
            entry.TargetType,
            entry.TargetId,
            entry.TargetDisplayName,
            entry.TargetTenantId,
            entry.Reason,
            entry.Changes?.ToJson(),
            entry.ActorContext.TenantId,
            entry.ActorContext.CorrelationId,
            entry.ActorContext.FlowId,
            timeProvider.GetUtcNow());
        dbContext.AdministrativeAudits.Add(audit);
    }

    /// <summary>
    /// The request's authenticated stakeholder (from the validated access token) is authoritative and yields
    /// the Identity user that owns it. When the caller also knows the Identity user, the two must agree.
    /// Without a stakeholder context, an explicitly supplied Identity user is used and the display name is
    /// taken from that user's stakeholder profile, preferring the request tenant.
    /// </summary>
    private async Task<AdministrativeAuditActor> ResolveActorAsync(
        AdministrativeAuditEntry entry,
        CancellationToken cancellationToken)
    {
        var context = entry.ActorContext;
        if (context.StakeholderId is { } stakeholderId)
        {
            var stakeholder = await dbContext.Stakeholders.AsNoTracking()
                .Where(item => item.Id == stakeholderId)
                .Select(item => new { item.AppUserId, item.FirstName, item.LastName })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    $"The acting administrator stakeholder '{stakeholderId}' could not be resolved.");
            if (entry.ActorUserId is { } expectedUserId && expectedUserId != stakeholder.AppUserId)
            {
                throw new InvalidOperationException(
                    "The acting administrator's user does not own the authenticated stakeholder context.");
            }

            return new AdministrativeAuditActor(
                stakeholder.AppUserId,
                stakeholderId,
                DisplayName(stakeholder.FirstName, stakeholder.LastName));
        }

        if (entry.ActorUserId is { } userId && userId != Guid.Empty)
        {
            var profile = await dbContext.Stakeholders.AsNoTracking()
                .Where(item => item.AppUserId == userId)
                .OrderBy(item => item.TenantId == context.TenantId ? 0 : 1)
                .ThenBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.Id)
                .Select(item => new { item.FirstName, item.LastName })
                .FirstOrDefaultAsync(cancellationToken);
            return new AdministrativeAuditActor(
                userId,
                null,
                profile is null ? AdministrativeAuditActor.FallbackDisplayName : DisplayName(profile.FirstName, profile.LastName));
        }

        throw new InvalidOperationException("An authenticated administrator is required to record an administrative audit.");
    }

    private static string DisplayName(string firstName, string lastName)
    {
        var name = $"{firstName} {lastName}".Trim();
        if (name.Length == 0)
        {
            return AdministrativeAuditActor.FallbackDisplayName;
        }

        return name.Length <= AdministrativeAudit.MaxDisplayNameLength ? name : name[..AdministrativeAudit.MaxDisplayNameLength];
    }
}
