using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Services;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed class ProcessMailtrapDeliveryWebhookHandler(
    IReadRepository<Provider> providerRepository,
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    IMailtrapWebhookSignatureValidator mailtrapWebhookSignatureValidator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessMailtrapDeliveryWebhookResult> HandleAsync(
        ProcessMailtrapDeliveryWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await mailtrapWebhookSignatureValidator.ValidateAsync(
            new MailtrapWebhookSignatureValidationRequest(command.SignatureHeader, command.RawPayload),
            cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus.InvalidSignature);
        }

        var provider = await providerRepository.FirstOrDefaultAsync(
                new ActiveProviderByTypeSpecification(ProviderType.Email),
                cancellationToken)
            ?? throw new InvalidOperationException("Mailtrap email provider is not active.");

        if (!string.Equals(provider.ProviderKey, "mailtrap", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The active email provider is not Mailtrap.");
        }

        var now = timeProvider.GetUtcNow();
        var hasPersistedWebhook = false;

        foreach (var webhookEvent in command.Events)
        {
            var existingWebhook = await emailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                new EmailDeliveryWebhookInboxByEventIdSpecification(provider.Id, webhookEvent.EventId),
                cancellationToken);
            if (existingWebhook is not null)
            {
                continue;
            }

            var rawEventPayload = System.Text.Json.JsonSerializer.Serialize(webhookEvent);
            var occurredAtUtc = DateTimeOffset.FromUnixTimeSeconds(webhookEvent.Timestamp);
            var inbox = EmailDeliveryWebhookInbox.Create(
                provider.Id,
                webhookEvent.EventId,
                webhookEvent.MessageId,
                webhookEvent.Email,
                webhookEvent.SendingStream,
                webhookEvent.SendingDomainName,
                rawEventPayload,
                occurredAtUtc,
                now);
            await emailDeliveryWebhookInboxRepository.AddAsync(inbox, cancellationToken);

            var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
                new EmailNotificationLogByProviderMessageIdSpecification(webhookEvent.MessageId),
                cancellationToken);
            if (emailNotificationLog is not null)
            {
                emailNotificationLog.MarkDelivered(occurredAtUtc, now);
                emailNotificationLogRepository.Update(emailNotificationLog);
            }

            hasPersistedWebhook = true;
        }

        if (!hasPersistedWebhook)
        {
            return new ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus.Duplicate);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus.Persisted);
    }
}
