namespace BackendProjectTemplate.Domain.Payments.Exceptions;

public sealed class BillingAccountResolutionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
