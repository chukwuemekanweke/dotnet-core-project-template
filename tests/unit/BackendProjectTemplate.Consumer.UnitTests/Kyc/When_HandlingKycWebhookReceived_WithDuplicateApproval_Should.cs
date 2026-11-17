using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class When_HandlingKycWebhookReceived_WithDuplicateApproval_Should
{
    [Fact]
    public async Task PublishOneLogicalVerificationEvent()
    {
        var context = new KycConsumerTestContext();
        var now = context.Clock.GetUtcNow();
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        var verification = KycVerification.Start(
            tenantId, stakeholder.Id, KycProviderKeys.Kycaid, "external-1", null, now.AddMinutes(-10));
        var first = CreateWebhook("request-1", verification, now);
        var duplicate = CreateWebhook("request-2", verification, now);
        context.WebhookInboxRepository.GetByIdAsync(first.Id, Arg.Any<CancellationToken>()).Returns(first);
        context.VerificationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns(verification);
        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = first.Id, RequestId = first.RequestId }, CancellationToken.None);

        context.WebhookInboxRepository.GetByIdAsync(duplicate.Id, Arg.Any<CancellationToken>()).Returns(duplicate);
        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = duplicate.Id, RequestId = duplicate.RequestId }, CancellationToken.None);

        first.ProcessingOutcome.ShouldBe(KycWebhookProcessingOutcome.StateTransitioned);
        duplicate.ProcessingOutcome.ShouldBe(KycWebhookProcessingOutcome.TerminalVerificationIgnored);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Any<StakeholderKycVerified>(), Arg.Any<CancellationToken>());
    }

    private static KycWebhookInbox CreateWebhook(string requestId, KycVerification verification, DateTimeOffset now) =>
        KycWebhookInbox.Create(
            KycProviderKeys.Kycaid, requestId, "VERIFICATION_COMPLETED", "verification-1", null,
            "applicant-1", verification.ExternalApplicantId, "completed", true, now);
}
