using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserRoleRequest(
    Guid ActorUserId,
    Guid TargetUserId,
    string RoleName,
    string Reason,
    ActorContext ActorContext);
