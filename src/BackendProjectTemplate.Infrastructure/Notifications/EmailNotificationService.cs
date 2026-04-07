using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Microsoft.Extensions.Options;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class EmailNotificationService(
    IReadRepository<EmailProvider> readRepository,
    IEnumerable<IEmailTransportProvider> transportProviders,
    IOptions<EmailNotificationsOptions> options) : IEmailNotificationService
{
    public async Task SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.NotificationContent is not EmailNotificationContent content)
        {
            throw new NotificationConfigurationException(
                $"Notification content '{command.NotificationContent.GetType().Name}' is not valid for email delivery.");
        }

        var provider = await readRepository.FirstOrDefaultAsync(new ActiveEmailProviderSpecification(), cancellationToken);

        if (provider is null)
        {
            throw new NotificationConfigurationException("No active email provider is configured.");
        }

        var transportProvider = transportProviders.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase));

        if (transportProvider is null)
        {
            throw new NotificationConfigurationException(
                $"No email transport implementation is registered for provider key '{provider.ProviderKey}'.");
        }

        var deliveryMessage = EmailNotificationContentFactory.Create(command, content, options.Value);
        await transportProvider.SendAsync(deliveryMessage, cancellationToken);
    }
}
