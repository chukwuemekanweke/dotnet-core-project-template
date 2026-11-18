using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Common.Auditing;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;

internal static class AdminUserControllerSupport
{
    public static bool TryGetActorUserId(ClaimsPrincipal principal, out Guid actorUserId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out actorUserId);

    public static ActorContext ActorContext(ICurrentActor currentActor) =>
        Domain.Common.Auditing.ActorContext.FromCurrentActor(currentActor);

    public static ActionResult<AdminUserMutationResponse> ToActionResult(
        ControllerBase controller,
        Guid userId,
        string action,
        AdminUserMutationResult result)
    {
        return result.Status switch
        {
            AdminUserMutationStatus.Succeeded => controller.Ok(
                new AdminUserMutationResponse(userId, action, true, result.Roles)),
            AdminUserMutationStatus.Unchanged => controller.Ok(
                new AdminUserMutationResponse(userId, action, false, result.Roles)),
            AdminUserMutationStatus.UserNotFound or AdminUserMutationStatus.SessionNotFound =>
                controller.Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            AdminUserMutationStatus.UnknownRole =>
                controller.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest),
            AdminUserMutationStatus.SelfProtection =>
                controller.Problem(result.Error, statusCode: StatusCodes.Status403Forbidden),
            AdminUserMutationStatus.LastAdministrator or AdminUserMutationStatus.IdentityFailure =>
                controller.Problem(result.Error, statusCode: StatusCodes.Status409Conflict),
            _ => controller.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
