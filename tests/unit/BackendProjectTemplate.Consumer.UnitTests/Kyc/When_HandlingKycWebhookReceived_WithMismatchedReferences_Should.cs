using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class When_HandlingKycWebhookReceived_WithMismatchedReferences_Should
{
    [Fact]
    public async Task PersistReferenceMismatchWithoutChangingVerification()
    {
        var context = new KycConsumerTestContext();
        var now = context.Clock.GetUtcNow();
        var verification = KycVerification.Start(
            Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "expected-external", "expected-applicant", now);
        var webhook = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid, "request-1", "VERIFICATION_COMPLETED", "verification-1", null,
            "other-applicant", "other-external", "completed", true, now);
        context.WebhookInboxRepository.GetByIdAsync(webhook.Id, Arg.Any<CancellationToken>()).Returns(webhook);
        context.VerificationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns(verification);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = webhook.Id, RequestId = webhook.RequestId },
            CancellationToken.None);

        verification.Status.ShouldBe(KycVerificationStatus.Pending);
        webhook.ProcessingOutcome.ShouldBe(KycWebhookProcessingOutcome.ReferenceMismatch);
        webhook.ProcessedAtUtc.ShouldBe(now);
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<StakeholderKycVerified>(), Arg.Any<CancellationToken>());
    }
}
