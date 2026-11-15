using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithProviderRejection_Should
{
    [Fact]
    public async Task ReleaseSessionRequestForImmediateRetry()
    {
        var context = new StartKycVerificationTestContext();
        context.ReturnActiveAttempt(null);
        KycVerification? added = null;
        context.VerificationRepository.AddAsync(Arg.Do<KycVerification>(value => added = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.Provider.CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns<KycSessionResult>(_ => throw new KycProviderRequestRejectedException("rejected"));

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.ProviderUnavailable);
        added.ShouldNotBeNull();
        added.IsActive.ShouldBeTrue();
        added.SessionRequestedAtUtc.ShouldBeNull();
        added.HasPendingSessionRequest(StartKycVerificationTestContext.Now).ShouldBeFalse();
        await context.UnitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
