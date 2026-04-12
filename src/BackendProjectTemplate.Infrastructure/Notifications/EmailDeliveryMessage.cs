namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed record EmailDeliveryMessage(
    string FromAddress,
    string FromName,
    string To,
    string Subject,
    string TextBody,
    string HtmlBody,
    string[]? Cc,
    string[]? Bcc);
