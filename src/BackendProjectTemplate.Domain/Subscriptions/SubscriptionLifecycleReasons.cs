namespace BackendProjectTemplate.Domain.Subscriptions;

public static class SubscriptionLifecycleReasons
{
    public const string PurchaseInitiated = "purchase_initiated";
    public const string InitialPaymentConfirmed = "initial_payment_confirmed";
    public const string AdministrativeAllocation = "administrative_allocation";
    public const string AllocationScheduled = "allocation_scheduled";
    public const string ScheduledAllocationActivated = "scheduled_allocation_activated";
    public const string ScheduledAllocationLapsed = "scheduled_allocation_lapsed";
    public const string CancellationScheduled = "cancellation_scheduled";
    public const string AdministrativeCancellationScheduled = "administrative_cancellation_scheduled";
    public const string AdministrativeImmediateCancellation = "administrative_immediate_cancellation";
    public const string PeriodEnded = "period_ended";
    public const string RenewalPaymentConfirmed = "renewal_payment_confirmed";
}
