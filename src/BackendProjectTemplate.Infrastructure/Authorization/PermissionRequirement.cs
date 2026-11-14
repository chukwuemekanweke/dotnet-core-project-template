using Microsoft.AspNetCore.Authorization;

namespace BackendProjectTemplate.Infrastructure.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
