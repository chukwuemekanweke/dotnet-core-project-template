using BackendProjectTemplate.Application.Kyc.Features.GetCurrentVerification;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.GetCurrentVerification;

public sealed class When_GettingCurrentKycVerification_Should
{
    [Fact]
    public async Task ReturnNotStartedForAnAuthenticatedStakeholderWithoutAnAttempt()
    {
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace");
        var verifications = Substitute.For<IRepository<KycVerification>>();
        var stakeholders = Substitute.For<IRepository<Stakeholder>>();
        stakeholders.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await new GetCurrentKycVerificationHandler(verifications, stakeholders).HandleAsync(
            new GetCurrentKycVerificationQuery(new ActorContext(stakeholder.Id, tenantId, "correlation", "flow")),
            CancellationToken.None);

        result.IsAuthenticated.ShouldBeTrue();
        result.Verification.ShouldNotBeNull();
        result.Verification.Status.ShouldBe(nameof(KycVerificationStatus.NotStarted));
        result.Verification.VerificationId.ShouldBeNull();
        result.Verification.VerificationUrl.ShouldBeNull();
        result.Verification.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnThePersistedTerminalAttemptWithoutExposingAnotherStakeholderSelector()
    {
        var now = new DateTimeOffset(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace");
        var verification = KycVerification.Start(
            tenantId,
            stakeholder.Id,
            KycProviderKeys.Kycaid,
            stakeholder.Id.ToString("N"),
            null,
            now);
        verification.ApplyProviderResult(KycVerificationTransition.Rejected, "applicant-1", now.AddMinutes(5));

        var verifications = Substitute.For<IRepository<KycVerification>>();
        var stakeholders = Substitute.For<IRepository<Stakeholder>>();
        stakeholders.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        verifications.FirstOrDefaultAsync(
                Arg.Any<ISpecification<KycVerification>>(),
                Arg.Any<CancellationToken>())
            .Returns(verification);

        var result = await new GetCurrentKycVerificationHandler(verifications, stakeholders).HandleAsync(
            new GetCurrentKycVerificationQuery(new ActorContext(stakeholder.Id, tenantId, "correlation", "flow")),
            CancellationToken.None);

        result.Verification.ShouldNotBeNull();
        result.Verification.VerificationId.ShouldBe(verification.Id);
        result.Verification.Status.ShouldBe(nameof(KycVerificationStatus.Rejected));
        result.Verification.VerificationUrl.ShouldBeNull();
        result.Verification.CompletedAtUtc.ShouldBe(now.AddMinutes(5));
    }
}
