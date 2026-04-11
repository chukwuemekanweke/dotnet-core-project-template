using Mailtrap;
using Mailtrap.Configuration;
using Mailtrap.Emails;
using Mailtrap.Emails.Requests;
using Microsoft.Extensions.Options;
using System.Net;

namespace BackendProjectTemplate.Infrastructure.Notifications;

internal sealed class MailtrapEmailTransportProvider(
    IOptions<EmailNotificationsOptions> options) : IEmailTransportProvider
{
    public string ProviderKey => EmailProviderKeys.Mailtrap;

    public async Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
    {
        var mailtrapOptions = options.Value.Mailtrap;
        mailtrapOptions.Validate();

        using var factory = new MailtrapClientFactory(new MailtrapClientOptions(mailtrapOptions.ApiToken));
        var client = factory.CreateClient();
        var request = CreateRequest(message);
        var emailClient = ResolveEmailClient(client, mailtrapOptions);
        var response = await emailClient.Send(request, cancellationToken);
    }

    internal static SendEmailRequest CreateRequest(EmailDeliveryMessage message)
    {
        var request = SendEmailRequest
            .Create()
            .From(message.FromAddress, message.FromName)
            .To(message.To)
            .Subject(message.Subject)
            .Text(string.Join(Environment.NewLine, message.Content))
            .Html(BuildHtmlBody(message.Content));

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

    private static string BuildHtmlBody(IEnumerable<string> contentLines)
    {
        var encodedLines = contentLines
            .Select(WebUtility.HtmlEncode)
            .ToArray();

        if (encodedLines.Length == 0)
        {
            return "<html><body></body></html>";
        }

        var body = string.Join(string.Empty, encodedLines.Select(line =>
            string.IsNullOrWhiteSpace(line)
                ? "<br />"
                : $"<p>{line}</p>"));

        return $"<html><body>{body}</body></html>";
    }
}
