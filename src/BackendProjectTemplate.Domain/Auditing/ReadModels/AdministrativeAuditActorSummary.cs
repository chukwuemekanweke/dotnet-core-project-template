namespace BackendProjectTemplate.Domain.Auditing.ReadModels;

public sealed record AdministrativeAuditActorSummary(
    Guid UserId,
    Guid? StakeholderId,
    string DisplayName);
