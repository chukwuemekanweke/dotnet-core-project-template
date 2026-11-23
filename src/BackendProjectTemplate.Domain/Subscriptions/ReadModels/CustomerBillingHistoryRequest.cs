using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record CustomerBillingHistoryRequest(
    Guid TenantId,
    BillingAccountOwnerType BillingAccountOwnerType,
    Guid StakeholderId,
    int Page,
    int PageSize);
