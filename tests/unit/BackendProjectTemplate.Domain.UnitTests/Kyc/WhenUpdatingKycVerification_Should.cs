using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Kyc;

public sealed class WhenUpdatingKycVerification_Should
{
    private static readonly DateTimeOffset StartedAt = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkVerifiedOnlyFromAuthoritativeCompletedApproval()
    {
        var verification = CreateVerification();
        verification.MarkSessionRequested(StartedAt);
        verification.AttachSession(new("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null));

        verification.ApplyProviderResult(KycVerificationTransition.InProgress, "applicant-1", StartedAt.AddMinutes(1));
        verification.Status.ShouldBe(KycVerificationStatus.InProgress);

        verification.ApplyProviderResult(KycVerificationTransition.Verified, "applicant-1", StartedAt.AddMinutes(2));
        verification.Status.ShouldBe(KycVerificationStatus.Verified);
        verification.CompletedAtUtc.ShouldBe(StartedAt.AddMinutes(2));
    }

    [Fact]
    public void NotTreatCompletedWithoutDecisionAsVerified()
    {
        var verification = CreateVerification();

        verification.ApplyProviderResult(KycVerificationTransition.NoChange, "applicant-1", StartedAt.AddMinutes(1));

        verification.Status.ShouldBe(KycVerificationStatus.Pending);
    }

    [Fact]
    public void PreventTerminalStatusDowngrade()
    {
        var verification = CreateVerification();
        verification.ApplyProviderResult(KycVerificationTransition.Verified, "applicant-1", StartedAt.AddMinutes(1));

        verification.ApplyProviderResult(KycVerificationTransition.InProgress, "applicant-1", StartedAt.AddMinutes(2));

        verification.Status.ShouldBe(KycVerificationStatus.Verified);
    }

    private static KycVerification CreateVerification() =>
        KycVerification.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "external-1", null, StartedAt);
}
