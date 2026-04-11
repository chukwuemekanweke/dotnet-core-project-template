using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class EmailNotificationService(
    IReadRepository<EmailProvider> emailProviderRepository,
    IReadRepository<EmailNotificationTemplate> emailNotificationTemplateRepository,
    IEnumerable<IEmailTransportProvider> transportProviders,
    IOptions<EmailNotificationsOptions> options) : IEmailNotificationService
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{:(?<key>[A-Za-z0-9_]+):\}\}", RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderFragmentPattern = new(@"\{\{:|:\}\}", RegexOptions.CultureInvariant);

    public async Task SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.NotificationContent is not EmailNotificationContent content)
        {
            throw new NotificationConfigurationException(
                $"Notification content '{command.NotificationContent.GetType().Name}' is not valid for email delivery.");
        }

        var provider = await emailProviderRepository.FirstOrDefaultAsync(new ActiveEmailProviderSpecification(), cancellationToken);

        if (provider is null)
        {
            throw new NotificationConfigurationException("No active email provider is configured.");
        }

        var template = await emailNotificationTemplateRepository.FirstOrDefaultAsync(
            new EmailNotificationTemplateByNotificationTypeSpecification(command.NotificationType),
            cancellationToken);

        if (template is null)
        {
            throw new NotificationConfigurationException(
                $"No email template is configured for notification type '{command.NotificationType}'.");
        }

        var transportProvider = transportProviders.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase));

        if (transportProvider is null)
        {
            throw new NotificationConfigurationException(
                $"No email transport implementation is registered for provider key '{provider.ProviderKey}'.");
        }

        var deliveryMessage = new EmailDeliveryMessage(
            options.Value.FromAddress,
            options.Value.FromName,
            content.To,
            RenderBody(template.Body, content.Content, command.NotificationType),
            RenderTemplate(template.Subject, content.Content, "subject", command.NotificationType),
            content.Cc,
            content.Bcc);
        await transportProvider.SendAsync(deliveryMessage, cancellationToken);
    }

    private static string[] RenderBody(
        string bodyTemplate,
        Dictionary<string, string> replacements,
        NotificationType notificationType)
    {
        var renderedBody = RenderTemplate(bodyTemplate, replacements, "body", notificationType);

        return renderedBody.Split(["\r\n", "\n"], StringSplitOptions.None);
    }

    private static string RenderTemplate(
        string template,
        Dictionary<string, string> replacements,
        string templatePart,
        NotificationType? notificationType)
    {
        var renderedTemplate = PlaceholderPattern.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;

            if (replacements.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new NotificationConfigurationException(
                $"The email {templatePart} template for notification type '{notificationType}' requires replacement key '{key}'.");
        });

        if (PlaceholderFragmentPattern.IsMatch(renderedTemplate))
        {
            throw new NotificationConfigurationException(
                $"The email {templatePart} template for notification type '{notificationType}' contains a malformed placeholder token. Expected format '{{{{:Key:}}}}'.");
        }

        return renderedTemplate;
    }
}
