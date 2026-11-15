namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_RecordingKycProviderVerificationReference_WithExistingReference_Should
{
    [Fact]
    public void KeepExistingReference()
    {
        var verification = KycVerificationTestData.CreateWithSession();

        verification.RecordProviderVerificationReference("verification-2");

        verification.ProviderVerificationReference.ShouldBe("verification-1");
    }
}
