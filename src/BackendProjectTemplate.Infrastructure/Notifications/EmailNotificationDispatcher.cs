using BackendProjectTemplate.Contracts.Commands.Notifications;
using BackendProjectTemplate.Domain.Common.Notifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class EmailNotificationDispatcher(
    IReadRepository<Provider> providerRepository,
    IReadRepository<EmailNotificationTemplate> emailNotificationTemplateRepository,
    IReadRepository<Tenant> tenantRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    IEnumerable<IEmailTransportProvider> transportProviders,
    IHostEnvironment hostEnvironment,
    IOptions<EmailNotificationsOptions> options,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EmailNotificationDispatcher>? logger = null) : IEmailNotificationService
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{:(?<key>[A-Za-z0-9_]+):\}\}", RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderFragmentPattern = new(@"\{\{:|:\}\}", RegexOptions.CultureInvariant);

    public async Task SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default)
    {
        var notificationsOptions = options.Value;

        if (command.NotificationContent is not EmailNotificationContent content)
        {
            throw new NotificationConfigurationException(
                $"Notification content '{command.NotificationContent.GetType().Name}' is not valid for email delivery.");
        }

        var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
            new EmailNotificationLogByMessageIdSpecification(command.MessageId),
            cancellationToken);
        if (emailNotificationLog?.IsSent is true)
        {
            logger?.LogWarning(
                "Skipping email notification for message {MessageId} because it has already been sent.",
                command.MessageId);

            return;
        }

        if (emailNotificationLog is null)
        {
            emailNotificationLog = EmailNotificationLog.Create(
                command.MessageId,
                command.TenantId,
                command.CountryId,
                command.NotificationType,
                NotificationContentObfuscator.Obfuscate(content.Content),
                content.To,
                JoinRecipients(content.Cc),
                JoinRecipients(content.Bcc));

            await emailNotificationLogRepository.AddAsync(emailNotificationLog, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        try
        {
            var provider = await providerRepository.FirstOrDefaultAsync(
                new ActiveProviderByTypeSpecification(ProviderType.Email),
                cancellationToken);

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

            var tenant = await tenantRepository.FirstOrDefaultAsync(
                new TenantByIdSpecification(command.TenantId),
                cancellationToken);

            if (tenant is null && command.TenantId != Guid.Empty)
            {
                tenant = await tenantRepository.FirstOrDefaultAsync(
                    new TenantByIdSpecification(Guid.Empty),
                    cancellationToken);
            }
            var brandKey = tenant?.BrandKey ?? notificationsOptions.DefaultBrandKey;

            var transportProvider = transportProviders.SingleOrDefault(candidate =>
                string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase));

            if (transportProvider is null)
            {
                throw new NotificationConfigurationException(
                    $"No email transport implementation is registered for provider key '{provider.ProviderKey}'.");
            }

            var renderedSubject = RenderTemplate(template.Subject, content.Content, "subject", command.NotificationType);
            var renderedBody = RenderTemplate(
                LoadNotificationTemplate(
                    notificationsOptions,
                    brandKey,
                    template.TemplateFileName,
                    command.NotificationType),
                content.Content,
                "body",
                command.NotificationType);
            var renderedHtmlBody = RenderTenantHtmlBody(
                LoadBaseTemplate(notificationsOptions, brandKey, command.NotificationType),
                renderedSubject,
                renderedBody,
                command.NotificationType);

            var deliveryMessage = new EmailDeliveryMessage(
                notificationsOptions.FromAddress,
                notificationsOptions.FromName,
                content.To,
                renderedSubject,
                renderedHtmlBody,
                content.Cc,
                content.Bcc);
            await transportProvider.SendAsync(deliveryMessage, cancellationToken);

            emailNotificationLog.MarkSent(timeProvider.GetUtcNow());
            emailNotificationLogRepository.Update(emailNotificationLog);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            emailNotificationLog.MarkFailed(ex.Message, timeProvider.GetUtcNow());
            emailNotificationLogRepository.Update(emailNotificationLog);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string? JoinRecipients(string[]? recipients) =>
        recipients is null || recipients.Length == 0
            ? null
            : string.Join(",", recipients);

    private string LoadNotificationTemplate(
        EmailNotificationsOptions notificationsOptions,
        string brandKey,
        string templateFileName,
        NotificationType notificationType)
    {
        if (templateFileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            throw new NotificationConfigurationException(
                $"Template file name '{templateFileName}' for notification type '{notificationType}' is invalid.");
        }

        var relativePath = Path.Combine(notificationsOptions.NotificationTemplatesFolder, templateFileName);
        return ReadTemplateFileWithFallback(notificationsOptions, brandKey, relativePath, "notification body", notificationType);
    }

    private string LoadBaseTemplate(
        EmailNotificationsOptions notificationsOptions,
        string brandKey,
        NotificationType notificationType) =>
        ReadTemplateFileWithFallback(
            notificationsOptions,
            brandKey,
            notificationsOptions.BaseTemplateFileName,
            "base template",
            notificationType);

    private string ReadTemplateFileWithFallback(
        EmailNotificationsOptions notificationsOptions,
        string brandKey,
        string relativePath,
        string templateKind,
        NotificationType notificationType)
    {
        var templateSetsRootPath = ResolveTemplateSetsRootPath(notificationsOptions.TemplateSetsRootPath);
        var brandKeyPath = Path.Combine(templateSetsRootPath, brandKey, relativePath);

        if (File.Exists(brandKeyPath))
        {
            return File.ReadAllText(brandKeyPath);
        }

        var defaultBrandKeyPath = Path.Combine(templateSetsRootPath, notificationsOptions.DefaultBrandKey, relativePath);
        if (File.Exists(defaultBrandKeyPath))
        {
            return File.ReadAllText(defaultBrandKeyPath);
        }

        throw new NotificationConfigurationException(
            $"No {templateKind} file was found for brand '{brandKey}' (or default brand '{notificationsOptions.DefaultBrandKey}') and notification type '{notificationType}'.");
    }

    private string ResolveTemplateSetsRootPath(string configuredRootPath) =>
        Path.IsPathRooted(configuredRootPath)
            ? configuredRootPath
            : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredRootPath));

    private static string RenderTenantHtmlBody(
        string tenantHtmlTemplate,
        string renderedSubject,
        string renderedBody,
        NotificationType notificationType)
    {
        var bodyHtml = RenderBodyHtml(renderedBody);

        return RenderTemplate(
            tenantHtmlTemplate,
            new Dictionary<string, string>
            {
                ["Subject"] = WebUtility.HtmlEncode(renderedSubject),
                ["BodyHtml"] = bodyHtml
            },
            "tenant base html",
            notificationType);
    }

    private static string RenderBodyHtml(string renderedBody)
    {
        if (renderedBody.Contains('<') && renderedBody.Contains('>'))
        {
            return renderedBody;
        }

        var bodyLines = renderedBody.Split(["\r\n", "\n"], StringSplitOptions.None);
        return string.Join(string.Empty, bodyLines.Select(line =>
            string.IsNullOrWhiteSpace(line)
                ? "<br />"
                : $"<p>{WebUtility.HtmlEncode(line)}</p>"));
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
