namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserEmailConfirmed(
    string EmailAddress) : BaseEvent;
