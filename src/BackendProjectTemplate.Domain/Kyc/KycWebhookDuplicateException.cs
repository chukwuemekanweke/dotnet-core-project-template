namespace BackendProjectTemplate.Domain.Kyc;

public sealed class KycWebhookDuplicateException(string message, Exception innerException) : Exception(message, innerException);
