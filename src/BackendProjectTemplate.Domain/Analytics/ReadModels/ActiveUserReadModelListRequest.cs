namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public sealed record ActiveUserReadModelListRequest(DateOnly Date, Guid? TenantId, int Page, int PageSize);
