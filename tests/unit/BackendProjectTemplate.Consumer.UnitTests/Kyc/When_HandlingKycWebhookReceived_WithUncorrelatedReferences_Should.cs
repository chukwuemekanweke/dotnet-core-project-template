using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class When_HandlingKycWebhookReceived_WithUncorrelatedReferences_Should
{
    [Fact]
    public async Task NotFallBackToExternalApplicantId()
    {
        var context = new KycConsumerTestContext();
        var webhook = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid, "request-1", "VERIFICATION_COMPLETED", "verification-old", "token-old", "applicant-1",
            "external-1", "completed", true, context.Clock.GetUtcNow());
        context.WebhookInboxRepository.GetByIdAsync(webhook.Id, Arg.Any<CancellationToken>()).Returns(webhook);
        context.VerificationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns((KycVerification?)null);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = webhook.Id, RequestId = webhook.RequestId },
            CancellationToken.None);

        webhook.ProcessedAtUtc.ShouldBe(context.Clock.GetUtcNow());
        webhook.ProcessingOutcome.ShouldBe(KycWebhookProcessingOutcome.UnknownVerification);
        await context.VerificationRepository.Received(2).FirstOrDefaultAsync(
            Arg.Is<ISpecification<KycVerification>>(specification =>
                specification is KycVerificationByProviderReferenceSpecification ||
                specification is KycVerificationBySessionReferenceSpecification),
            Arg.Any<CancellationToken>());
        await context.VerificationRepository.Received(2).FirstOrDefaultAsync(
            Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>());
        await context.StakeholderRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }
}
