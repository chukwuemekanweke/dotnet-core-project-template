using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class When_HandlingKycWebhookReceived_WithCallbackOnlyVerificationReference_Should
{
    private static readonly DateTimeOffset StartedAt = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordReferenceForReconciliation()
    {
        var context = new KycConsumerTestContext();
        var verification = KycVerification.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "external-1", null, StartedAt);
        verification.MarkSessionRequested(StartedAt);
        verification.AttachSession(new(null, "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
        var webhook = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid, "request-1", "VERIFICATION_STATUS_CHANGED", "verification-1", "token-1", "applicant-1",
            "external-1", "pending", null, StartedAt.AddMinutes(5));
        context.WebhookInboxRepository.GetByIdAsync(webhook.Id, Arg.Any<CancellationToken>()).Returns(webhook);
        context.VerificationRepository.FirstOrDefaultAsync(
                Arg.Is<ISpecification<KycVerification>>(specification => specification is KycVerificationBySessionReferenceSpecification),
                Arg.Any<CancellationToken>())
            .Returns(verification);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = webhook.Id, RequestId = webhook.RequestId },
            CancellationToken.None);

        verification.ProviderVerificationReference.ShouldBe("verification-1");
        verification.Status.ShouldBe(KycVerificationStatus.InProgress);
    }
}
