namespace BackendProjectTemplate.Domain.Authentication.Administration;

public sealed record AdminUserMutationResult(
    AdminUserMutationStatus Status,
    IReadOnlyList<string>? Roles = null,
    string? Error = null);
