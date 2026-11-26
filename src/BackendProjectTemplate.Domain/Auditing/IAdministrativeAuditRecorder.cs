namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// Stages an administrative audit record on the current write unit of work. It never saves or commits: the
/// calling administrative mutation persists the record with its own changes in the same transaction, so the
/// mutation cannot commit without its audit. Call it only after the mutation succeeded and actually changed
/// state, never for failed, rejected, unchanged or replayed operations.
/// </summary>
public interface IAdministrativeAuditRecorder
{
    Task RecordAsync(AdministrativeAuditEntry entry, CancellationToken cancellationToken);
}
