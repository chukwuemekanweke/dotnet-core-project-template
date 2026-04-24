namespace BackendProjectTemplate.Infrastructure.Payments.Stripe;

internal interface IStripeClient
{
    Task<HttpResponseMessage> CreateCheckoutSessionAsync(
        object payload,
        CancellationToken cancellationToken);
}
