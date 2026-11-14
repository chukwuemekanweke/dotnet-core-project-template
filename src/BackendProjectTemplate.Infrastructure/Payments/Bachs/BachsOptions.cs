namespace BackendProjectTemplate.Infrastructure.Payments.Bachs;

public sealed class BachsOptions
{
    public const string SectionName = "Payments:Bachs";

    public string BaseUrl { get; set; } = "https://sandbox-api.bachs.io";
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public int CheckoutExpirationMinutes { get; set; } = 60;
    public int WebhookTimestampToleranceSeconds { get; set; } = 300;
}
