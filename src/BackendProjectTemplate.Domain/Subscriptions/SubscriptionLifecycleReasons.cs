namespace BackendProjectTemplate.Domain.Subscriptions;

public static class SubscriptionLifecycleReasons
{
    public const string PurchaseInitiated = "purchase_initiated";
    public const string InitialPaymentConfirmed = "initial_payment_confirmed";
    public const string AdministrativeAllocation = "administrative_allocation";
    public const string CancellationScheduled = "cancellation_scheduled";
    public const string PeriodEnded = "period_ended";
}
