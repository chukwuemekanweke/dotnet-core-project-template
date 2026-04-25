namespace BackendProjectTemplate.Domain.Common.Exceptions;

public sealed class PaymentProviderResolutionException(string message) : Exception(message);
