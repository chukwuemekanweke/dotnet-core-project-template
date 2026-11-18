using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserSessionRequest(
    Guid ActorUserId,
    Guid TargetUserId,
    Guid SessionId,
    string Reason,
    ActorContext ActorContext);
