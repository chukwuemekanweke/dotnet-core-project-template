namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

internal interface IBachsClient
{
    Task<BachsCheckoutSessionResponse> CreateCheckoutSessionAsync(
        BachsCreateCheckoutSessionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<BachsCheckoutSessionResponse> GetCheckoutSessionAsync(
        string checkoutId,
        CancellationToken cancellationToken);
}
