using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.UnitTests.Kyc.StartVerification;

internal sealed class StartKycVerificationTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    public StartKycVerificationTestContext()
    {
        TenantId = Guid.CreateVersion7();
        Stakeholder = Stakeholder.Create(Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        StakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(Stakeholder);
        Provider.ProviderKey.Returns(KycProviderKeys.Kycaid);
        Provider.CreateSessionAsync(Arg.Any<KycSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new KycSessionResult("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
    }

    public Guid TenantId { get; }
    public Stakeholder Stakeholder { get; }
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<KycVerification> VerificationRepository { get; } = Substitute.For<IRepository<KycVerification>>();
    public IKycProviderService Provider { get; } = Substitute.For<IKycProviderService>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public StartKycVerificationCommand Command => new(new ActorContext(Stakeholder.Id, TenantId, "correlation-1", "flow-1"));

    public KycVerification CreateAttempt(DateTimeOffset startedAt) =>
        KycVerification.Start(TenantId, Stakeholder.Id, KycProviderKeys.Kycaid, Stakeholder.Id.ToString("N"), null, startedAt);

    public void ReturnActiveAttempt(KycVerification? verification) =>
        VerificationRepository.FirstOrDefaultAsync(
                Arg.Is<ISpecification<KycVerification>>(specification => specification is ActiveKycVerificationSpecification),
                Arg.Any<CancellationToken>())
            .Returns(verification);

    public StartKycVerificationHandler CreateHandler() =>
        new(StakeholderRepository, VerificationRepository, [Provider], UnitOfWork, new FixedTimeProvider(Now));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
