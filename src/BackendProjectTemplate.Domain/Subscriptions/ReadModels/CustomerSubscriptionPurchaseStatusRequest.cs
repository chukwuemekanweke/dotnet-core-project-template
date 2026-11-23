using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record CustomerSubscriptionPurchaseStatusRequest(
    Guid PurchaseId,
    Guid TenantId,
    BillingAccountOwnerType BillingAccountOwnerType,
    Guid StakeholderId);
