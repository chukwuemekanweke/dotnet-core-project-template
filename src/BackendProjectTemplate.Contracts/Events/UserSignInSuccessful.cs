namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserSignInSuccessful(
    Guid UserId,
    string EmailAddress,
    string IpAddress,
    string UserAgent) : BaseEvent;
