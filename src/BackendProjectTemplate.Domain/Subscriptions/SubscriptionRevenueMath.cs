namespace BackendProjectTemplate.Domain.Subscriptions;

public static class SubscriptionRevenueMath
{
    /// <summary>
    /// Normalizes immutable period prices to monthly recurring revenue without premature rounding.
    /// Values remain decimal minor-currency units; API serialization preserves the resulting decimal precision.
    /// </summary>
    public static decimal NormalizeMrr(decimal price, BillingInterval interval) => interval switch
    {
        BillingInterval.Monthly => price,
        BillingInterval.Yearly => price / 12m,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
    };
}
