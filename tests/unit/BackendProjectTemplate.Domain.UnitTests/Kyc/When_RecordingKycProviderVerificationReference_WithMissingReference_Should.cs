namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_RecordingKycProviderVerificationReference_WithMissingReference_Should
{
    [Fact]
    public void StoreCallbackReference()
    {
        var verification = KycVerificationTestData.CreateWithSession(verificationReference: null);

        verification.RecordProviderVerificationReference(" verification-1 ");

        verification.ProviderVerificationReference.ShouldBe("verification-1");
    }
}
