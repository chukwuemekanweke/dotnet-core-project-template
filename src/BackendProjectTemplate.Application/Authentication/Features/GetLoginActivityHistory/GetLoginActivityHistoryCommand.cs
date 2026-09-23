using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Authentication.Features.GetLoginActivityHistory;

public sealed record GetLoginActivityHistoryCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
