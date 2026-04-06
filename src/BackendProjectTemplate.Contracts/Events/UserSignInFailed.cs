namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserSignInFailed(
    Guid? UserId,
    string EmailAddress,
    string IpAddress,
    string UserAgent,
    string FailureReason) : BaseEvent;
