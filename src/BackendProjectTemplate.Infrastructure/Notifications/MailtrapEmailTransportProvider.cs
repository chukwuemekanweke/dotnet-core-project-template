using Mailtrap;
using Mailtrap.Configuration;
using Mailtrap.Emails;
using Mailtrap.Emails.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class MailtrapEmailTransportProvider(
    IOptions<EmailNotificationsOptions> options,
    ILogger<MailtrapEmailTransportProvider> logger) : IEmailTransportProvider
{
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .Build();

    public string ProviderKey => EmailProviderKeys.Mailtrap;

    public async Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
    {
        var mailtrapOptions = options.Value.Mailtrap;
        mailtrapOptions.Validate();
        logger.LogWarning(
            "Mailtrap API token diagnostic. Api Token={ApiToken} Use Bulk Api={UseBulkApi} Inbox Id={InboxId}",
            mailtrapOptions.ApiToken,
            mailtrapOptions.UseBulkApi,
            mailtrapOptions.InboxId);

        using var factory = new MailtrapClientFactory(new MailtrapClientOptions(mailtrapOptions.ApiToken));
        var client = factory.CreateClient();
        var request = CreateRequest(message);
        var emailClient = ResolveEmailClient(client, mailtrapOptions);

        await RetryPipeline.ExecuteAsync(
            async token =>
            {
                await emailClient.Send(request, token);
            },
            cancellationToken);
    }

    internal static SendEmailRequest CreateRequest(EmailDeliveryMessage message)
    {
        var request = SendEmailRequest
            .Create()
            .From(message.FromAddress, message.FromName)
            .To(message.To)
            .Subject(message.Subject)
            .Html(message.HtmlBody);

        if (message.Cc is not null)
        {
            foreach (var cc in message.Cc)
            {
                request.Cc(cc);
            }
        }

        if (message.Bcc is not null)
        {
            foreach (var bcc in message.Bcc)
            {
                request.Bcc(bcc);
            }
        }

        return request;
    }

    private static ISendEmailClient ResolveEmailClient(
        IMailtrapClient client,
        EmailNotificationsOptions.MailtrapOptions options)
    {
        if (options.InboxId.HasValue)
        {
            return client.Test(options.InboxId.Value);
        }

        return options.UseBulkApi ? client.Bulk() : client.Email();
    }

}
