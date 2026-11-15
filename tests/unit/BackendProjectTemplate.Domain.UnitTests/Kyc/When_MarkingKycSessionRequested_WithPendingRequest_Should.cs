using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Kyc.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class When_MarkingKycSessionRequested_WithPendingRequest_Should
{
    [Fact]
    public void BlockNewRequestUntilLeaseElapses()
    {
        var verification = KycVerificationTestData.CreatePending();
        var requestedAt = KycVerificationTestData.StartedAt;
        verification.MarkSessionRequested(requestedAt);
        var withinLease = requestedAt + KycVerification.SessionProvisioningLease - TimeSpan.FromSeconds(1);
        var afterLease = requestedAt + KycVerification.SessionProvisioningLease;

        verification.HasPendingSessionRequest(withinLease).ShouldBeTrue();
        Should.Throw<AggregateStateException>(() => verification.MarkSessionRequested(withinLease));

        verification.HasPendingSessionRequest(afterLease).ShouldBeFalse();
        verification.MarkSessionRequested(afterLease);
        verification.SessionRequestedAtUtc.ShouldBe(afterLease);
    }
}
