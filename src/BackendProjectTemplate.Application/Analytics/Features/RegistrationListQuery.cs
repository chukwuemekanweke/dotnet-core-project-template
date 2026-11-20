namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed record RegistrationListQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? TenantId,
    int Page,
    int PageSize);
