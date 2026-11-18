using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.RevokeAdminUserSession;
using BackendProjectTemplate.Application.Authentication.Features.RevokeAllAdminUserSessions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users/{userId:guid}/sessions")]
public sealed class AdminUserSessionsController(
    AdminSecurityMutationValidator validator,
    RevokeAdminUserSessionHandler revokeHandler,
    RevokeAllAdminUserSessionsHandler revokeAllHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpDelete("{sessionId:guid}")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.RevokeUserSession)]
    public async Task<ActionResult<AdminUserMutationResponse>> Revoke(
        [FromRoute] Guid userId,
        [FromRoute] Guid sessionId,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!AdminUserControllerSupport.TryGetActorUserId(User, out var actorUserId)) return Unauthorized();

        var result = await revokeHandler.HandleAsync(new RevokeAdminUserSessionCommand(
            actorUserId, userId, sessionId, request.Reason,
            AdminUserControllerSupport.ActorContext(currentActor)), cancellationToken);
        return AdminUserControllerSupport.ToActionResult(this, userId, "revoke-session", result);
    }

    [HttpDelete]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.RevokeAllUserSessions)]
    public async Task<ActionResult<AdminUserMutationResponse>> RevokeAll(
        [FromRoute] Guid userId,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!AdminUserControllerSupport.TryGetActorUserId(User, out var actorUserId)) return Unauthorized();

        var result = await revokeAllHandler.HandleAsync(new RevokeAllAdminUserSessionsCommand(
            actorUserId, userId, request.Reason,
            AdminUserControllerSupport.ActorContext(currentActor)), cancellationToken);
        return AdminUserControllerSupport.ToActionResult(this, userId, "revoke-all-sessions", result);
    }
}
