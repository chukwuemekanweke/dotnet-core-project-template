namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserCreated(
    Guid UserId,
    string EmailAddress) : BaseEvent;
