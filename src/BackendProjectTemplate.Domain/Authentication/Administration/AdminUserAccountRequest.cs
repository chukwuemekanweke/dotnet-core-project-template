using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserAccountRequest(
    Guid ActorUserId,
    Guid TargetUserId,
    string Reason,
    ActorContext ActorContext);
