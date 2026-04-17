namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserSignInSuccessful(
    string EmailAddress,
    string IpAddress,
    string UserAgent) : BaseEvent;
