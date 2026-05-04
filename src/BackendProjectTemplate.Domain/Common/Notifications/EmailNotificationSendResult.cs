namespace BackendProjectTemplate.Domain.Common.Notifications;

public sealed record EmailNotificationSendResult(string ProviderKey, string ProviderMessageId);
