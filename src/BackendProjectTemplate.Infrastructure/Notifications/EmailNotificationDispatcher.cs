using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class EmailNotificationDispatcher(
    IReadRepository<EmailProvider> emailProviderRepository,
    IReadRepository<EmailNotificationTemplate> emailNotificationTemplateRepository,
    IReadRepository<TenantEmailBaseTemplate> tenantEmailBaseTemplateRepository,
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

        var tenantBaseTemplate = await tenantEmailBaseTemplateRepository.FirstOrDefaultAsync(
            new TenantEmailBaseTemplateByTenantIdSpecification(command.TenantId),
            cancellationToken);

        if (tenantBaseTemplate is null)
        {
            throw new NotificationConfigurationException(
                $"No tenant email base template is configured for tenant '{command.TenantId}'.");
        }

        var transportProvider = transportProviders.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase));

        if (transportProvider is null)
        {
            throw new NotificationConfigurationException(
                $"No email transport implementation is registered for provider key '{provider.ProviderKey}'.");
        }

        var renderedSubject = RenderTemplate(template.Subject, content.Content, "subject", command.NotificationType);
        var renderedBody = RenderTemplate(template.Body, content.Content, "body", command.NotificationType);
        var renderedHtmlBody = RenderTenantHtmlBody(
            tenantBaseTemplate.HtmlTemplate,
            renderedSubject,
            renderedBody,
            command.NotificationType);

        var deliveryMessage = new EmailDeliveryMessage(
            options.Value.FromAddress,
            options.Value.FromName,
            content.To,
            renderedSubject,
            renderedBody,
            renderedHtmlBody,
            content.Cc,
            content.Bcc);
        await transportProvider.SendAsync(deliveryMessage, cancellationToken);
    }

    private static string RenderTenantHtmlBody(
        string tenantHtmlTemplate,
        string renderedSubject,
        string renderedBody,
        NotificationType notificationType)
    {
        var bodyLines = renderedBody.Split(["\r\n", "\n"], StringSplitOptions.None);
        var bodyHtml = string.Join(string.Empty, bodyLines.Select(line =>
            string.IsNullOrWhiteSpace(line)
                ? "<br />"
                : $"<p>{WebUtility.HtmlEncode(line)}</p>"));

        return RenderTemplate(
            tenantHtmlTemplate,
            new Dictionary<string, string>
            {
                ["Subject"] = WebUtility.HtmlEncode(renderedSubject),
                ["BodyText"] = WebUtility.HtmlEncode(renderedBody),
                ["BodyHtml"] = bodyHtml
            },
            "tenant base html",
            notificationType);
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
