using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BackendProjectTemplate.Consumer.Kyc;

public sealed class KycWebhookReceivedHandler(
    IRepository<KycWebhookInbox> webhookInboxRepository,
    IRepository<KycVerification> verificationRepository,
    IRepository<Stakeholder> stakeholderRepository,
    IKycVerificationResultNormalizer resultNormalizer,
    IEventPublisher eventPublisher,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<KycWebhookReceivedHandler> logger) : BaseMessageHandler<KycWebhookReceived>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(KycWebhookReceived message, CancellationToken cancellationToken)
    {
        if (message.WebhookInboxId == Guid.Empty || string.IsNullOrWhiteSpace(message.RequestId))
        {
            throw new CannotProcessMessageNonTransientException(
                "KycWebhookReceived must contain a webhook inbox id and provider request id.");
        }

        var inbox = await webhookInboxRepository.GetByIdAsync(message.WebhookInboxId, cancellationToken);
        if (inbox is null || !string.Equals(inbox.RequestId, message.RequestId, StringComparison.Ordinal))
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process KYC webhook inbox '{message.WebhookInboxId}'.");
        }

        if (inbox.ProcessedAtUtc.HasValue)
        {
            return;
        }

        var verification = await ResolveVerificationAsync(inbox, cancellationToken);
        var now = Clock.GetUtcNow();
        if (verification is null)
        {
            MarkProcessed(inbox, KycWebhookProcessingOutcome.UnknownVerification, now);
            logger.LogWarning("KYC webhook {KycWebhookInboxId} did not match a verification.", inbox.Id);
            return;
        }

        if ((inbox.ExternalApplicantId is not null && inbox.ExternalApplicantId != verification.ExternalApplicantId) ||
            (verification.ProviderApplicantReference is not null &&
             !string.Equals(verification.ProviderApplicantReference, inbox.ApplicantReference, StringComparison.Ordinal)))
        {
            MarkProcessed(inbox, KycWebhookProcessingOutcome.ReferenceMismatch, now);
            logger.LogWarning("KYC webhook {KycWebhookInboxId} references did not correlate.", inbox.Id);
            return;
        }

        if (!verification.IsActive)
        {
            MarkProcessed(inbox, KycWebhookProcessingOutcome.TerminalVerificationIgnored, now);
            logger.LogInformation(
                "KYC webhook {KycWebhookInboxId} was ignored because verification {KycVerificationId} is terminal.",
                inbox.Id,
                verification.Id);
            return;
        }

        verification.RecordProviderVerificationReference(inbox.VerificationReference);

        var transitioned = false;
        Stakeholder? stakeholder = null;
        if (inbox.EventType == "FORM_URL_EXPIRED")
        {
            verification.MarkFormUrlExpired(now);
            transitioned = true;
        }
        else
        {
            var transition = resultNormalizer.Normalize(inbox.ProviderStatus, inbox.Verified);
            if (transition == KycVerificationTransition.Verified)
            {
                stakeholder = await stakeholderRepository.GetByIdAsync(verification.StakeholderId, cancellationToken);
                if (stakeholder is null || stakeholder.TenantId != verification.TenantId)
                {
                    throw new InvalidOperationException(
                        $"Stakeholder for KYC verification '{verification.Id}' could not be loaded.");
                }
            }

            transitioned = verification.ApplyProviderResult(transition, inbox.ApplicantReference, now);
        }

        if (transitioned && verification.Status == KycVerificationStatus.Verified)
        {
            stakeholder!.MarkVerified();
            if (verification.TryMarkVerifiedEventPublished(now))
            {
                await eventPublisher.PublishAsync(
                    new StakeholderKycVerified
                    {
                        TenantId = verification.TenantId,
                        StakeholderId = verification.StakeholderId,
                        KycVerificationId = verification.Id,
                        OccuredAt = now
                    },
                    cancellationToken);
            }
        }

        MarkProcessed(
            inbox,
            transitioned ? KycWebhookProcessingOutcome.StateTransitioned : KycWebhookProcessingOutcome.NoStateTransition,
            now);
    }

    private void MarkProcessed(KycWebhookInbox inbox, KycWebhookProcessingOutcome outcome, DateTimeOffset now)
    {
        inbox.MarkProcessed(outcome, now);
        CustomTelemetryContext.AddCustomEvent(
            "kyc_webhook_processed",
            new Dictionary<string, string> { ["processing_outcome"] = outcome.ToString() });
    }

    private async Task<KycVerification?> ResolveVerificationAsync(KycWebhookInbox inbox, CancellationToken cancellationToken)
    {
        if (inbox.VerificationReference is not null)
        {
            var byVerification = await verificationRepository.FirstOrDefaultAsync(
                new KycVerificationByProviderReferenceSpecification(KycProviderKeys.Kycaid, inbox.VerificationReference), cancellationToken);
            if (byVerification is not null)
            {
                return byVerification;
            }
        }

        if (inbox.SessionReference is not null)
        {
            var bySession = await verificationRepository.FirstOrDefaultAsync(
                new KycVerificationBySessionReferenceSpecification(KycProviderKeys.Kycaid, inbox.SessionReference), cancellationToken);
            if (bySession is not null)
            {
                return bySession;
            }
        }

        return null;
    }
}
