using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;

public sealed record GetSecurityActivityHistoryCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
