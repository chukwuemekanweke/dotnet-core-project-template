namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_ReleasingKycSessionRequest_WithRejectedProviderRequest_Should
{
    [Fact]
    public void AllowImmediateRetry()
    {
        var verification = KycVerificationTestData.CreatePending();
        var requestedAt = KycVerificationTestData.StartedAt;
        verification.MarkSessionRequested(requestedAt);

        verification.ReleaseSessionRequest();

        verification.SessionRequestedAtUtc.ShouldBeNull();
        verification.HasPendingSessionRequest(requestedAt).ShouldBeFalse();
        Should.NotThrow(() => verification.MarkSessionRequested(requestedAt));
    }
}
