namespace BackendProjectTemplate.Domain.Subscriptions;

/// <summary>
/// The single recurring-revenue normalization rule used by every subscription analytics calculation.
/// Values remain decimal minor-currency units without intermediate rounding; API serialization preserves
/// the resulting decimal precision. Currencies are never converted or combined.
/// </summary>
public static class SubscriptionRevenueMath
{
    public const int MonthsPerYear = 12;

    /// <summary>The number of calendar months one billing period of <paramref name="interval"/> covers.</summary>
    public static int MonthsPerInterval(BillingInterval interval) => interval switch
    {
        BillingInterval.Monthly => 1,
        BillingInterval.Yearly => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Billing interval is not supported for revenue normalization.")
    };

    /// <summary>
    /// Normalizes an immutable period price (or a sum of prices sharing one interval) to monthly recurring
    /// revenue. The rule is linear, so normalizing a database-side sum equals summing normalized prices.
    /// </summary>
    public static decimal NormalizeMrr(decimal price, BillingInterval interval) =>
        price / MonthsPerInterval(interval);

    /// <summary>Annual recurring revenue is always derived from normalized MRR.</summary>
    public static decimal ToArr(decimal mrr) => mrr * MonthsPerYear;
}
