using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.GetAdminUserDetail;
using BackendProjectTemplate.Application.Authentication.Features.ListAdminUsers;
using BackendProjectTemplate.Application.Authentication.Features.LockAdminUser;
using BackendProjectTemplate.Application.Authentication.Features.UnlockAdminUser;
using BackendProjectTemplate.Domain.Authentication.Administration;
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
[Route(EndpointUrl.AdminUsers.Route)]
public sealed class AdminUsersController(
    ListAdminUsersValidator listValidator,
    AdminSecurityMutationValidator mutationValidator,
    ListAdminUsersHandler listHandler,
    GetAdminUserDetailHandler detailHandler,
    LockAdminUserHandler lockHandler,
    UnlockAdminUserHandler unlockHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.UsersRead)]
    [ProducesResponseType<AdminUserListPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserListPage>> List(
        [FromQuery] ListAdminUsersRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await listHandler.HandleAsync(new ListAdminUsersQuery(
            request.Search, request.Role, request.IsLocked, request.TenantId, request.Page, request.PageSize), cancellationToken));
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = PermissionPolicyNames.UsersRead)]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDetail>> Get(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var detail = await detailHandler.HandleAsync(new GetAdminUserDetailQuery(userId), cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("{userId:guid}/lock")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.LockUser)]
    public Task<ActionResult<AdminUserMutationResponse>> Lock(
        [FromRoute] Guid userId,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken) =>
        ChangeLockStateAsync(userId, request, locked: true, cancellationToken);

    [HttpPost("{userId:guid}/unlock")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    [RequireStepUpMfa(StepUpMfaOperations.UnlockUser)]
    public Task<ActionResult<AdminUserMutationResponse>> Unlock(
        [FromRoute] Guid userId,
        [FromBody] AdminSecurityMutationRequest request,
        CancellationToken cancellationToken) =>
        ChangeLockStateAsync(userId, request, locked: false, cancellationToken);

    private async Task<ActionResult<AdminUserMutationResponse>> ChangeLockStateAsync(
        Guid userId,
        AdminSecurityMutationRequest request,
        bool locked,
        CancellationToken cancellationToken)
    {
        var validation = await mutationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        if (!AdminUserControllerSupport.TryGetActorUserId(User, out var actorUserId))
        {
            return Unauthorized();
        }

        var actorContext = AdminUserControllerSupport.ActorContext(currentActor);
        var result = locked
            ? await lockHandler.HandleAsync(new LockAdminUserCommand(actorUserId, userId, request.Reason, actorContext), cancellationToken)
            : await unlockHandler.HandleAsync(new UnlockAdminUserCommand(actorUserId, userId, request.Reason, actorContext), cancellationToken);
        return AdminUserControllerSupport.ToActionResult(this, userId, locked ? "lock" : "unlock", result);
    }
}
