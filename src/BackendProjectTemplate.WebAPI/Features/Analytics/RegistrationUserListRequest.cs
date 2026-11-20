namespace BackendProjectTemplate.WebAPI.Features.Analytics;

public sealed record RegistrationUserListRequest(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? TenantId = null,
    int Page = 1,
    int PageSize = 20);
