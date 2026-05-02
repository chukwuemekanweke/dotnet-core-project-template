namespace BackendProjectTemplate.Domain.Payments;

public static class KnownPaymentTransactionChangeReasons
{
    public const string PaymentInitiated = "payment_initiated";
    public const string ReconciliationConfirmedSuccess = "reconciliation_confirmed_success";
    public const string ReconciliationConfirmedFailure = "reconciliation_confirmed_failure";
    public const string ReconciliationConfirmedExpired = "reconciliation_confirmed_expired";
    public const string ReconciliationStillProcessing = "reconciliation_still_processing";
}
