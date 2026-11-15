namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionAllocationOperationConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
