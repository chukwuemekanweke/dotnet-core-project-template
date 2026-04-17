namespace BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public PolicyOptions AuthPublicPolicy { get; init; } = new(5, 1, 0);

    public PolicyOptions AuthenticatedUserPolicy { get; init; } = new(30, 1, 0);

    public PolicyOptions GlobalFallbackPolicy { get; init; } = new(100, 1, 0);

    public sealed record PolicyOptions(
        int PermitLimit,
        int WindowMinutes,
        int QueueLimit);
}
