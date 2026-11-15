namespace BackendProjectTemplate.Domain.Kyc;

public sealed class KycVerificationConcurrencyException(string message, Exception innerException) : Exception(message, innerException);
