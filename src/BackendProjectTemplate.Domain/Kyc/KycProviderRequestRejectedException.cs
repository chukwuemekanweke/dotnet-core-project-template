namespace BackendProjectTemplate.Domain.Kyc;

/// <summary>
/// The KYC provider definitively did not perform the requested operation (integration disabled, or the provider
/// answered with a client error), so retrying it cannot create a duplicate provider-side resource.
/// </summary>
public sealed class KycProviderRequestRejectedException : Exception
{
    public KycProviderRequestRejectedException(string message)
        : base(message)
    {
    }

    public KycProviderRequestRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
