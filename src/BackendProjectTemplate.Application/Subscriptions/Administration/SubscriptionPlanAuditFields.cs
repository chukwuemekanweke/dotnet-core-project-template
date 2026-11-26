using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Application.Subscriptions.Administration;

/// <summary>
/// The safe, explicitly selected plan fields captured in administrative audit change payloads. Prices are keyed
/// by billing interval name so the payload stays readable and deterministic.
/// </summary>
internal static class SubscriptionPlanAuditFields
{
    public static SortedDictionary<string, decimal> Prices(SubscriptionPlan plan) =>
        new(
            plan.Prices.ToDictionary(price => price.BillingInterval.ToString(), price => price.Price),
            StringComparer.Ordinal);

    public static string[] Features(SubscriptionPlan plan) => [.. plan.Features];
}
