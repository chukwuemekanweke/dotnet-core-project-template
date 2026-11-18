namespace BackendProjectTemplate.Domain.Authentication.Administration;

public enum AdminUserMutationStatus
{
    Succeeded = 1,
    Unchanged = 2,
    UserNotFound = 3,
    SessionNotFound = 4,
    UnknownRole = 5,
    SelfProtection = 6,
    LastAdministrator = 7,
    IdentityFailure = 8
}

public sealed record AdminUserMutationResult(
    AdminUserMutationStatus Status,
    IReadOnlyList<string>? Roles = null,
    string? Error = null);
