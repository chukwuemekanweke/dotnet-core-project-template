using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

internal static class KycVerificationTestData
{
    public static readonly DateTimeOffset StartedAt = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    public static KycVerification CreatePending() =>
        KycVerification.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "external-1", null, StartedAt);

    public static KycVerification CreateWithSession(string? verificationReference = "verification-1")
    {
        var verification = CreatePending();
        verification.MarkSessionRequested(StartedAt);
        verification.AttachSession(new(verificationReference, "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
        return verification;
    }
}
