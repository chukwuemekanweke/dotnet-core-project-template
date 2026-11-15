using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

public sealed class When_StartingKycVerification_WithNoActiveAttempt_Should
{
    [Fact]
    public async Task PersistSessionRequestBeforeCallingProvider()
    {
        var context = new StartKycVerificationTestContext();
        context.ReturnActiveAttempt(null);
        KycVerification? added = null;
        DateTimeOffset? requestedAtProviderCall = null;
        context.VerificationRepository.AddAsync(Arg.Do<KycVerification>(value => added = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.Provider.When(provider => provider.CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>()))
            .Do(_ => requestedAtProviderCall = added!.SessionRequestedAtUtc);

        var result = await context.CreateHandler().HandleAsync(context.Command, CancellationToken.None);

        result.Status.ShouldBe(StartKycVerificationStatus.Success);
        result.Verification!.VerificationUrl.ShouldBe("https://forms.kycaid.com/token-1");
        requestedAtProviderCall.ShouldBe(StartKycVerificationTestContext.Now);
        Received.InOrder(() =>
        {
            context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            context.Provider.CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>());
            context.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
        added!.ProviderSessionReference.ShouldBe("token-1");
    }
}
