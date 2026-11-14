namespace BackendProjectTemplate.Domain.Payments;

public enum RecurringPaymentAuthorizationStatus
{
    Pending = 1,
    Active = 2,
    Revoked = 3,
    Expired = 4
}
