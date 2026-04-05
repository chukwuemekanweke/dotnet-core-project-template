namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserEmailConfirmed(
    Guid UserId,
    string EmailAddress) : BaseEvent;
