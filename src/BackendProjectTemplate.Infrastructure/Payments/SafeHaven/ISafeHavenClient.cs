namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal interface ISafeHavenClient
{
    Task<HttpResponseMessage> CreateVirtualAccountPaymentAsync(
        object payload,
        CancellationToken cancellationToken);
}
