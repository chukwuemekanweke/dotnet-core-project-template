namespace BackendProjectTemplate.Domain.Payments.Exceptions;

public sealed class BillingAccountOwnerNotFoundException(string message) : Exception(message);
