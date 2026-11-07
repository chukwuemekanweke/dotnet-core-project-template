namespace BackendProjectTemplate.Application.Authentication.Features.ListPasskeys;

public sealed record ListPasskeysQuery(Guid AppUserId, Guid StakeholderId, Guid TenantId);
