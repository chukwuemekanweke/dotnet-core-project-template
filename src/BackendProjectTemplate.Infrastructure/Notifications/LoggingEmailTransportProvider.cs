using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class LoggingEmailTransportProvider(ILogger<LoggingEmailTransportProvider> logger) : IEmailTransportProvider
{
    public string ProviderKey => EmailProviderKeys.Logging;

    public Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending email via logging provider. To={To} Subject={Subject} CcCount={CcCount} BccCount={BccCount} HtmlLength={HtmlLength}",
            message.To,
            message.Subject,
            message.Cc?.Length ?? 0,
            message.Bcc?.Length ?? 0,
            message.HtmlBody.Length);

        return Task.CompletedTask;
    }
}
