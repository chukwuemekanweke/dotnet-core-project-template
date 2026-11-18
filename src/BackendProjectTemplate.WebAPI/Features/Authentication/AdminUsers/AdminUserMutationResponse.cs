namespace BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;

public sealed record AdminUserMutationResponse(
    Guid UserId,
    string Action,
    bool Changed,
    IReadOnlyList<string>? Roles = null);
