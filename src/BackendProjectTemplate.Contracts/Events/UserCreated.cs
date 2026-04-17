namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserCreated(
    string EmailAddress) : BaseEvent;
