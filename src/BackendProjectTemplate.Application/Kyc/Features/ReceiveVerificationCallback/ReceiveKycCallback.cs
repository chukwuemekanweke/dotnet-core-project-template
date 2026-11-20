using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;

namespace BackendProjectTemplate.Application.Kyc.Features.ReceiveVerificationCallback;

public sealed record ReceiveKycCallbackCommand(
    string RequestId,
    string EventType,
    string? VerificationReference,
    string? SessionReference,
    string? ApplicantReference,
    string? ExternalApplicantId,
    string ProviderStatus,
    bool? Verified);

public sealed class ReceiveKycCallbackHandler(
    IRepository<KycWebhookInbox> inboxRepository,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ReceiveKycCallbackStatus> HandleAsync(ReceiveKycCallbackCommand command, CancellationToken cancellationToken)
    {
        if (await inboxRepository.AnyAsync(
            new KycWebhookByRequestIdSpecification(KycProviderKeys.Kycaid, command.RequestId), cancellationToken))
        {
            return ReceiveKycCallbackStatus.Duplicate;
        }

        var now = timeProvider.GetUtcNow();
        var inbox = KycWebhookInbox.Create(
            KycProviderKeys.Kycaid,
            command.RequestId,
            command.EventType,
            command.VerificationReference,
            command.SessionReference,
            command.ApplicantReference,
            command.ExternalApplicantId,
            command.ProviderStatus,
            command.Verified,
            now);
        await inboxRepository.AddAsync(inbox, cancellationToken);
        await eventPublisher.PublishAsync(
            new KycWebhookReceived
            {
                WebhookInboxId = inbox.Id,
                RequestId = inbox.RequestId,
                OccuredAt = now
            },
            cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (KycWebhookDuplicateException)
        {
            unitOfWork.DiscardChanges();
            return ReceiveKycCallbackStatus.Duplicate;
        }

        return ReceiveKycCallbackStatus.Persisted;
    }
}
