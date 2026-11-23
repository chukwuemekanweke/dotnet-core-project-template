namespace BackendProjectTemplate.Domain.Subscriptions.Exceptions;

public sealed class SubscriptionPurchaseIdempotencyKeyReusedException(string message) : Exception(message);
