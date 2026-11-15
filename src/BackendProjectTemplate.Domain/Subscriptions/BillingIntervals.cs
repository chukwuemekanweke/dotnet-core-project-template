namespace BackendProjectTemplate.Domain.Subscriptions;

public static class BillingIntervals
{
    public static DateTimeOffset CalculatePeriodEnd(BillingInterval billingInterval, DateTimeOffset periodStartUtc) =>
        billingInterval switch
        {
            BillingInterval.Monthly => periodStartUtc.AddMonths(1),
            BillingInterval.Yearly => periodStartUtc.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(billingInterval), billingInterval, "Unsupported billing interval.")
        };
}
