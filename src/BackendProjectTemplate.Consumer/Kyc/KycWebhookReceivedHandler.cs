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
        if (verification is null ||
            (inbox.ExternalApplicantId is not null && inbox.ExternalApplicantId != verification.ExternalApplicantId) ||
            (verification.ProviderApplicantReference is not null &&
             !string.Equals(verification.ProviderApplicantReference, inbox.ApplicantReference, StringComparison.Ordinal)))
        {
            inbox.MarkProcessed(now);
            return;
        }

        verification.RecordProviderVerificationReference(inbox.VerificationReference);

        if (inbox.EventType == "FORM_URL_EXPIRED")
        {
            if (verification.IsActive)
            {
                verification.MarkFormUrlExpired(now);
            }
        }
        else
        {
            verification.ApplyProviderResult(inbox.ProviderStatus, inbox.Verified, inbox.ApplicantReference, now);
        }

        if (verification.Status == KycVerificationStatus.Verified)
        {
            var stakeholder = await stakeholderRepository.GetByIdAsync(verification.StakeholderId, cancellationToken);
            if (stakeholder is not null && stakeholder.TenantId == verification.TenantId)
            {
                stakeholder.MarkVerified();
            }
        }

        inbox.MarkProcessed(now);
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
