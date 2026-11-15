namespace BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;

public sealed record AllocateSubscriptionResult(
    AllocateSubscriptionStatus Status,
    AllocateSubscriptionResponse? Response,
    string? Error)
{
    public static AllocateSubscriptionResult Failed(AllocateSubscriptionStatus status, string error) => new(status, null, error);
}
