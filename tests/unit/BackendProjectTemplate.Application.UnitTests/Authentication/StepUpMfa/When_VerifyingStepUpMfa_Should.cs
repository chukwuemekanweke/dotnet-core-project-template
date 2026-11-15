using BackendProjectTemplate.Application.Authentication;
using BackendProjectTemplate.Application.Authentication.Features.VerifyStepUpMfa;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.StepUpMfa;

public sealed class When_VerifyingStepUpMfa_Should
{
    [Fact]
    public async Task IssueAnOperationAndSessionBoundProofForAValidAuthenticatorCode()
    {
        var context = CreateContext(out var user, out var actorContext, out var sessionId);
        var proofStore = Substitute.For<IStepUpMfaProofStore>();
        var expiresAtUtc = context.Clock.GetUtcNow().AddMinutes(2);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.IdentityService.GetSecurityStampAsync(user).Returns("security-stamp");
        proofStore.TryClaimAuthenticatorCodeAsync(user.Id, "123456", TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(true);
        proofStore.IssueAsync(Arg.Any<StepUpMfaProof>(), Arg.Any<CancellationToken>())
            .Returns(new StepUpMfaProofIssueResult("opaque-proof", expiresAtUtc));
        var sut = CreateHandler(context, proofStore);

        var result = await sut.HandleAsync(
            new VerifyStepUpMfaCommand(
                TwoFactorVerificationMethod.Authenticator,
                "123456",
                StepUpMfaOperations.AllocateSubscription,
                new string('a', 64),
                sessionId,
                actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(VerifyStepUpMfaStatus.Success);
        result.AuthorizationToken.ShouldBe("opaque-proof");
        result.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        await proofStore.Received(1).IssueAsync(
            Arg.Is<StepUpMfaProof>(proof =>
                proof.AppUserId == user.Id &&
                proof.StakeholderId == actorContext.StakeholderId &&
                proof.SessionId == sessionId &&
                proof.Operation == StepUpMfaOperations.AllocateSubscription &&
                proof.RequestFingerprint == new string('a', 64)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectRecoveryCodesAndMissingEnrollmentWithoutIssuingAProof()
    {
        var context = CreateContext(out var user, out var actorContext, out var sessionId);
        var proofStore = Substitute.For<IStepUpMfaProofStore>();
        var sut = CreateHandler(context, proofStore);

        var recoveryCodeResult = await sut.HandleAsync(
            new VerifyStepUpMfaCommand(
                TwoFactorVerificationMethod.RecoveryCode,
                "ABCDE-FGHIJ",
                StepUpMfaOperations.ActivateProvider,
                new string('a', 64),
                sessionId,
                actorContext),
            CancellationToken.None);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(false);
        var enrollmentResult = await sut.HandleAsync(
            new VerifyStepUpMfaCommand(
                TwoFactorVerificationMethod.Authenticator,
                "123456",
                StepUpMfaOperations.ActivateProvider,
                new string('a', 64),
                sessionId,
                actorContext),
            CancellationToken.None);

        recoveryCodeResult.Status.ShouldBe(VerifyStepUpMfaStatus.InvalidMethod);
        enrollmentResult.Status.ShouldBe(VerifyStepUpMfaStatus.EnrollmentRequired);
        await proofStore.DidNotReceiveWithAnyArgs().IssueAsync(default!, default);
    }

    [Fact]
    public async Task RejectAReplayedAuthenticatorTimeStepWithoutIssuingAProof()
    {
        var context = CreateContext(out var user, out var actorContext, out var sessionId);
        var proofStore = Substitute.For<IStepUpMfaProofStore>();
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        proofStore.TryClaimAuthenticatorCodeAsync(user.Id, "123456", TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateHandler(context, proofStore);

        var result = await sut.HandleAsync(
            new VerifyStepUpMfaCommand(
                TwoFactorVerificationMethod.Authenticator,
                "123456",
                StepUpMfaOperations.ActivateProvider,
                new string('a', 64),
                sessionId,
                actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(VerifyStepUpMfaStatus.CodeReplayed);
        await proofStore.DidNotReceiveWithAnyArgs().IssueAsync(default!, default);
    }

    private static AuthenticationFlowTestContext CreateContext(
        out AppUser user,
        out ActorContext actorContext,
        out Guid sessionId)
    {
        var context = new AuthenticationFlowTestContext();
        user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        sessionId = Guid.CreateVersion7();
        actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        return context;
    }

    private static VerifyStepUpMfaHandler CreateHandler(
        AuthenticationFlowTestContext context,
        IStepUpMfaProofStore proofStore) => new(
        new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
        context.IdentityService,
        proofStore,
        Options.Create(new StepUpMfaOptions()),
        context.CustomTelemetryContext,
        context.UnitOfWork,
        context.Clock);
}
