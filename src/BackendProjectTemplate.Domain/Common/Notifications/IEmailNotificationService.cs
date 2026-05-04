using BackendProjectTemplate.Contracts.Commands.Notifications;

namespace BackendProjectTemplate.Domain.Common.Notifications;

public interface IEmailNotificationService
{
    Task<EmailNotificationSendResult?> SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default);
}
