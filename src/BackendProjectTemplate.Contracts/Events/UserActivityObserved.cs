namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserActivityObserved(
    DateOnly ActivityDate,
    string ActivityType) : BaseEvent;
