namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionConcurrencyConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
