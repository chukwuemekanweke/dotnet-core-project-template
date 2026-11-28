namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

public sealed class BachsOptions
{
    public const string SectionName = "Payments:Bachs";

    public string BaseUrl { get; set; } = "https://sandbox-api.bachs.io";
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public BachsReturnUrlsOptions ReturnUrls { get; set; } = new();
    public int CheckoutExpirationMinutes { get; set; } = 60;
    public int WebhookTimestampToleranceSeconds { get; set; } = 300;

    public sealed class BachsReturnUrlsOptions
    {
        public BachsCheckoutReturnUrlsOptions WalletTopUp { get; set; } = new();
        public BachsCheckoutReturnUrlsOptions SubscriptionPurchase { get; set; } = new();
    }

    public sealed class BachsCheckoutReturnUrlsOptions
    {
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
