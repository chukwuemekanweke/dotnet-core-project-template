using BackendProjectTemplate.Application.Kyc.Features.ReconcileVerifications;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.ReconcileVerifications;

internal sealed class KycReconciliationTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    public KycReconciliationTestContext()
    {
        Provider.ProviderKey.Returns(KycProviderKeys.Kycaid);
    }

    public IRepository<KycVerification> VerificationRepository { get; } = Substitute.For<IRepository<KycVerification>>();
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IKycProviderService Provider { get; } = Substitute.For<IKycProviderService>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public (Stakeholder Stakeholder, KycVerification Verification) CreateAttempt(string verificationReference)
    {
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        var startedAt = Now.AddHours(-2);
        var verification = KycVerification.Start(tenantId, stakeholder.Id, KycProviderKeys.Kycaid, stakeholder.Id.ToString("N"), null, startedAt);
        verification.MarkSessionRequested(startedAt);
        verification.AttachSession(new(verificationReference, $"token-{verificationReference}", new Uri("https://forms.kycaid.com/token"), null));
        StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        return (stakeholder, verification);
    }

    public void ReturnEligible(params KycVerification[] verifications) =>
        VerificationRepository.ListAsync(Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns(verifications.ToList());

    public Task<ReconcileKycVerificationsResult> RunAsync() =>
        new KycReconciliationService(
                VerificationRepository,
                StakeholderRepository,
                [Provider],
                EventPublisher,
                UnitOfWork,
                new FixedTimeProvider(Now),
                NullLogger<KycReconciliationService>.Instance)
            .HandleAsync(Now.AddMinutes(-30), Now.AddMinutes(-30), 50, CancellationToken.None);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
