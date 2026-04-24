namespace BackendProjectTemplate.Infrastructure.Payments.Stripe;

public sealed class StripeOptions
{
    public const string SectionName = "Payments:Stripe";

    public string BaseUrl { get; set; } = "https://placeholder.stripe.local";
}
