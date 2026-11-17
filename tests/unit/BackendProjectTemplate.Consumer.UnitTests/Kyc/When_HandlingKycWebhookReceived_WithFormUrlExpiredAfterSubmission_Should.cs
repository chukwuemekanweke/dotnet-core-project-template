using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using Shouldly;

namespace BackendProjectTemplate.Consumer.UnitTests.Kyc;

public sealed class When_HandlingKycWebhookReceived_WithFormUrlExpiredAfterSubmission_Should
{
    private static readonly DateTimeOffset StartedAt = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task KeepAttemptOpenAndCorrelatable()
    {
        var context = new KycConsumerTestContext();
        var verification = KycVerification.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), KycProviderKeys.Kycaid, "external-1", null, StartedAt);
        verification.MarkSessionRequested(StartedAt);
        verification.AttachSession(new("verification-1", "token-1", new Uri("https://forms.kycaid.com/token-1"), null));
        verification.ApplyProviderResult(KycVerificationTransition.InProgress, "applicant-1", StartedAt.AddMinutes(5));
        var webhook = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid, "request-1", "FORM_URL_EXPIRED", "verification-1", "token-1", "applicant-1",
            "external-1", "expired", null, StartedAt.AddMinutes(10));
        context.WebhookInboxRepository.GetByIdAsync(webhook.Id, Arg.Any<CancellationToken>()).Returns(webhook);
        context.VerificationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<KycVerification>>(), Arg.Any<CancellationToken>())
            .Returns(verification);

        await context.CreateHandler().HandleAsync(
            new KycWebhookReceived { WebhookInboxId = webhook.Id, RequestId = webhook.RequestId },
            CancellationToken.None);

        verification.Status.ShouldBe(KycVerificationStatus.InProgress);
        verification.CompletedAtUtc.ShouldBeNull();
        verification.ProviderVerificationReference.ShouldBe("verification-1");
        webhook.ProcessedAtUtc.ShouldBe(context.Clock.GetUtcNow());
    }
}
