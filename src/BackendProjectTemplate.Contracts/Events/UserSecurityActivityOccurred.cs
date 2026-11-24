namespace BackendProjectTemplate.Contracts.Events;

public sealed record UserSecurityActivityOccurred(
    string ActivityType,
    string IpAddress,
    string UserAgent) : BaseEvent;
