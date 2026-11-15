using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Infrastructure.Persistence;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Kyc;

public sealed class When_FilteringPendingKycReconciliations_WithMixedAttempts_Should
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectOnlyStaleActiveCorrelatableAttemptsDueForCheck()
    {
        var eligible = CreateAttempt(Now.AddHours(-2), "verification-1");
        var recentlyChecked = CreateAttempt(Now.AddHours(-2), "verification-2");
        recentlyChecked.RecordReconciliation(Now.AddMinutes(-5));
        var tooRecent = CreateAttempt(Now.AddMinutes(-5), "verification-3");
        var withoutReference = CreateAttempt(Now.AddHours(-2), null);
        var completed = CreateAttempt(Now.AddHours(-2), "verification-4");
        completed.ApplyProviderResult("completed", true, "applicant-1", Now.AddHours(-1));
        var specification = new PendingKycReconciliationSpecification(Now.AddMinutes(-30), Now.AddMinutes(-30), 50);

        var result = SpecificationEvaluator
            .GetQuery(new[] { eligible, recentlyChecked, tooRecent, withoutReference, completed }.AsQueryable(), specification)
            .ToList();

        result.ShouldBe([eligible]);
    }

    private static KycVerification CreateAttempt(DateTimeOffset startedAt, string? verificationReference)
    {
        var verification = KycVerification.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "external-1", null, startedAt);
        verification.MarkSessionRequested(startedAt);
        verification.AttachSession(new(verificationReference, $"token-{Guid.CreateVersion7():N}", new Uri("https://forms.kycaid.com/token"), null));
        return verification;
    }
}
