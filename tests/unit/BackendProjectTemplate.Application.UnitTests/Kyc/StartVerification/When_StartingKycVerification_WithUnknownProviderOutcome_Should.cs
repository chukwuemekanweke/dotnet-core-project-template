using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithUnknownProviderOutcome_Should
{
    public static TheoryData<Exception> Failures => new()
    {
        new HttpRequestException("bad gateway"),
        new TaskCanceledException("timed out"),
        new InvalidOperationException("untrusted url")
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task KeepSessionRequestPendingWithoutFailingAttempt(Exception failure)
    {
        var context = new StartKycVerificationTestContext();
        context.ReturnActiveAttempt(null);
        KycVerification? added = null;
        context.VerificationRepository.AddAsync(Arg.Do<KycVerification>(value => added = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.Provider.CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns<KycSessionResult>(_ => throw failure);

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.ProviderUnavailable);
        added.ShouldNotBeNull();
        added.Status.ShouldBe(KycVerificationStatus.Pending);
        added.HasPendingSessionRequest(StartKycVerificationTestContext.Now).ShouldBeTrue();
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
