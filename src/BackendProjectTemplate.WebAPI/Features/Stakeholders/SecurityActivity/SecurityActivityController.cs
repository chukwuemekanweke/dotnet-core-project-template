using Asp.Versioning;
using BackendProjectTemplate.Application.Authentication.Features.GetSecurityActivityHistory;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route($"{EndpointUrl.Stakeholders.Route}/me/security-activity")]
public sealed class SecurityActivityController(
    GetSecurityActivityHistoryHandler handler,
    GetSecurityActivityValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SecurityActivityHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SecurityActivityHistoryResponse>> Handle(
        [FromQuery] GetSecurityActivityRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        if (!Guid.TryParse(currentActor.ActorId, out _) ||
            !currentActor.TenantId.HasValue || currentActor.TenantId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(
            new GetSecurityActivityHistoryCommand(
                request.Limit,
                request.Cursor,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status == GetSecurityActivityHistoryStatus.NotAuthenticated
            ? Unauthorized()
            : Ok(new SecurityActivityHistoryResponse(result.Activities, result.NextCursor));
    }
}
