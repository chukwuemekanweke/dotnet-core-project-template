namespace BackendProjectTemplate.Domain.Kyc;

public sealed class KycActiveVerificationConflictException(string message, Exception innerException) : Exception(message, innerException)
{
}
