namespace BackendProjectTemplate.Contracts.Payments;

public enum PaymentStatus
{
    PendingInitiation = 1,
    Initiated = 2,
    AwaitingCustomerAction = 3,
    Processing = 4,
    Succeeded = 5,
    Failed = 6,
    Cancelled = 7,
    Expired = 8
}
