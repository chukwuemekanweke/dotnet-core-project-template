using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class WhenHandlingKycWebhookReceived_Should
{
    private static readonly DateTimeOffset StartedAt = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("pending", null, KycVerificationStatus.InProgress, false)]
    [InlineData("completed", true, KycVerificationStatus.Verified, true)]
    [InlineData("completed", false, KycVerificationStatus.Rejected, false)]
    public async Task ProcessDurableCallbackAndRecordMessage(
        string providerStatus,
        bool? verified,
        KycVerificationStatus expectedStatus,
        bool expectedStakeholderVerification)
    {
        var context = new KycConsumerTestContext();
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Ada", "Lovelace");
        var verification = KycVerification.Start(tenantId, stakeholder.Id, KycProviderKeys.Kycaid, stakeholder.Id.ToString("N"), null, StartedAt);
        verification.MarkSessionRequested(StartedAt);
        verification.AttachSession(new("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
        var webhook = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid,
            "request-1",
            "VERIFICATION_COMPLETED",
            "verification-1",
            "token-1",
            "applicant-1",
            verification.ExternalApplicantId,
            providerStatus,
            verified,
            StartedAt);
        context.WebhookInboxRepository.GetByIdAsync(webhook.Id, Arg.Any<CancellationToken>()).Returns(webhook);
        context.VerificationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>()).Returns(verification);
        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = webhook.Id, RequestId = webhook.RequestId },
            CancellationToken.None);

        verification.Status.ShouldBe(expectedStatus);
        stakeholder.IsVerified.ShouldBe(expectedStakeholderVerification);
        webhook.ProcessedAtUtc.ShouldBe(context.Clock.GetUtcNow());
        webhook.ProcessingOutcome.ShouldBe(KycWebhookProcessingOutcome.StateTransitioned);
        if (expectedStatus == KycVerificationStatus.Verified)
        {
            verification.VerifiedEventPublishedAtUtc.ShouldBe(context.Clock.GetUtcNow());
            await context.EventPublisher.Received(1).PublishAsync(
                Arg.Is<StakeholderKycVerified>(value =>
                    value.KycVerificationId == verification.Id &&
                    value.StakeholderId == stakeholder.Id &&
                    value.TenantId == tenantId),
                Arg.Any<CancellationToken>());
        }
        else
        {
            await context.EventPublisher.DidNotReceive().PublishAsync(
                Arg.Any<StakeholderKycVerified>(),
                Arg.Any<CancellationToken>());
        }
        await context.MessageInboxRepository.Received(1).AddAsync(Arg.Any<MessageInbox>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
