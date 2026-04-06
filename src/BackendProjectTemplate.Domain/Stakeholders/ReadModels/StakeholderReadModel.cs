namespace BackendProjectTemplate.Domain.Stakeholders.ReadModels;

public sealed record StakeholderReadModel(
    Guid StakeholderId,
    Guid AppUserId,
    Guid TenantId,
    Guid CountryId,
    Guid StakeholderTypeId);
