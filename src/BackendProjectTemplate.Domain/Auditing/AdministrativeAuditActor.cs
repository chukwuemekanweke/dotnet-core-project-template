namespace BackendProjectTemplate.Domain.Auditing;

/// <summary>
/// The authenticated administrator performing an action, resolved server-side. <see cref="UserId"/> is the
/// ASP.NET Identity user; <see cref="StakeholderId"/> is the stakeholder the request acted as, when known.
/// The two identifiers are never interchangeable.
/// </summary>
public sealed record AdministrativeAuditActor(Guid UserId, Guid? StakeholderId, string DisplayName)
{
    /// <summary>Deterministic, non-sensitive name used when the administrator has no stakeholder profile name.</summary>
    public const string FallbackDisplayName = "Unnamed administrator";
}
