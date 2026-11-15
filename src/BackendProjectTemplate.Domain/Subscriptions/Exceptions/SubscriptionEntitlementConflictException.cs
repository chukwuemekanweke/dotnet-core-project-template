namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionEntitlementConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
