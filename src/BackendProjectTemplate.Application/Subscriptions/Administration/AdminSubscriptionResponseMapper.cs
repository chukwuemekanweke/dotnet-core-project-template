using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Administration;

public static class AdminSubscriptionResponseMapper
{
    public static AdminSubscriptionSummaryResponse ToResponse(AdminSubscriptionListItemReadModel item) =>
        new(
            item.SubscriptionId,
            item.TenantId,
            item.TenantName,
            item.BillingAccountId,
            item.BillingAccountOwnerType.ToString(),
            item.StakeholderId,
            item.BillingAccountDisplayName,
            item.PlanId,
            item.PlanCode,
            item.PlanName,
            item.Status.ToString(),
            item.CurrentPeriodStartUtc,
            item.CurrentPeriodEndUtc,
            item.EntitlementStartUtc,
            item.EntitlementEndUtc,
            item.NextBillingAtUtc,
            item.AutoRenew,
            item.CancelAtPeriodEnd,
            item.CreatedAtUtc,
            item.Origin.ToString(),
            item.AllocationSource?.ToString());

    public static AdminSubscriptionPlanResponse ToResponse(AdminSubscriptionPlanReadModel plan) =>
        new(
            plan.PlanId,
            plan.Code,
            plan.Name,
            plan.Price,
            plan.CurrencyId,
            plan.CurrencyCode,
            plan.BillingInterval.ToString(),
            plan.IsActive,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);

    public static AdminSubscriptionPlanResponse ToResponse(SubscriptionPlan plan, Currency currency) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Price,
            plan.CurrencyId,
            currency.CurrencyCode,
            plan.BillingInterval.ToString(),
            plan.IsActive,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);
}
