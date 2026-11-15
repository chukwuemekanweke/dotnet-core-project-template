namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionPlanCodeConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
