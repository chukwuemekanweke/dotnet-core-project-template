namespace BackendProjectTemplate.Infrastructure.Notifications;

internal interface IEmailTransportProvider
{
    string ProviderKey { get; }

    Task SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default);
}
