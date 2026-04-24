namespace BackendProjectTemplate.Infrastructure.Payments.Credo;

internal interface ICredoClient
{
    Task<HttpResponseMessage> CreatePaymentLinkAsync(
        object payload,
        CancellationToken cancellationToken);
}
