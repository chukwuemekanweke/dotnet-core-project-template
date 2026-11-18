using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.AssignAdminUserRole;
using BackendProjectTemplate.Application.Authentication.Features.GetAdminUserRoles;
using BackendProjectTemplate.Application.Authentication.Features.RemoveAdminUserRole;
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
[Route("api/v{version:apiVersion}/admin/users/{userId:guid}/roles")]
public sealed class AdminUserRolesController(
    AdminSecurityMutationValidator validator,
    GetAdminUserRolesHandler getHandler,
    AssignAdminUserRoleHandler assignHandler,
    RemoveAdminUserRoleHandler removeHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.UsersRead)]
    public async Task<ActionResult<GetAdminUserRolesResponse>> Get(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetAdminUserRolesQuery(userId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{roleName}")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.AssignUserRole)]
    public Task<ActionResult<AdminUserMutationResponse>> Assign(
        [FromRoute] Guid userId,
        [FromRoute] string roleName,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(userId, roleName, request, assign: true, cancellationToken);

    [HttpDelete("{roleName}")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.RemoveUserRole)]
    public Task<ActionResult<AdminUserMutationResponse>> Remove(
        [FromRoute] Guid userId,
        [FromRoute] string roleName,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken) =>
        ChangeAsync(userId, roleName, request, assign: false, cancellationToken);

    private async Task<ActionResult<AdminUserMutationResponse>> ChangeAsync(
        Guid userId,
        string roleName,
        AdminSecurityMutationRequest request,
        bool assign,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        if (!AdminUserControllerSupport.TryGetActorUserId(User, out var actorUserId)) return Unauthorized();

        var actorContext = AdminUserControllerSupport.ActorContext(currentActor);
        var result = assign
            ? await assignHandler.HandleAsync(new AssignAdminUserRoleCommand(
                actorUserId, userId, roleName, request.Reason, actorContext), cancellationToken)
            : await removeHandler.HandleAsync(new RemoveAdminUserRoleCommand(
                actorUserId, userId, roleName, request.Reason, actorContext), cancellationToken);
        return AdminUserControllerSupport.ToActionResult(this, userId, assign ? "assign-role" : "remove-role", result);
    }
}
