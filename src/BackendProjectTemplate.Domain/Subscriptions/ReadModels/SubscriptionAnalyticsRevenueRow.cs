namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public sealed record SubscriptionAnalyticsRevenueRow(
    Guid CurrencyId,
    string CurrencyCode,
    decimal Mrr);
