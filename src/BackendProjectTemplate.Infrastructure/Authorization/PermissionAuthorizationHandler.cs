using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BackendProjectTemplate.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(
    IUserPermissionService permissionService,
    ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!ApplicationPermissions.All.Contains(requirement.Permission))
        {
            logger.LogWarning("Authorization denied because permission {Permission} is not registered", requirement.Permission);
            return;
        }

        var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(subject, out var userId))
        {
            return;
        }

        try
        {
            if (await permissionService.HasPermissionAsync(userId, requirement.Permission, CancellationToken.None))
            {
                context.Succeed(requirement);
                return;
            }

            logger.LogInformation(
                "Permission authorization denied for user {UserId} and permission {Permission}",
                userId,
                requirement.Permission);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Permission authorization failed closed for user {UserId} and permission {Permission}",
                userId,
                requirement.Permission);
        }
    }
}
