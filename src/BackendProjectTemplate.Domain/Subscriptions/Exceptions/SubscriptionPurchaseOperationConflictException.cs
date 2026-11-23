namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionPurchaseOperationConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
