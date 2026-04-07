namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed record EmailDeliveryMessage(
    string FromAddress,
    string FromName,
    string To,
    string[] Content,
    string Subject,
    string[]? Cc,
    string[]? Bcc);
