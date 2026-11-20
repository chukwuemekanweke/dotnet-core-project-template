using Asp.Versioning;
using BackendProjectTemplate.Application.Analytics.Features;
using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Analytics;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Policy = PermissionPolicyNames.UserAnalyticsRead)]
[ExcludeFromUserActivity]
public sealed class AdminAnalyticsUsersController(
    RegistrationUserListValidator registrationValidator,
    ActiveUserListValidator activeUserValidator,
    ListRegisteredUsersHandler registrationHandler,
    ListActiveUsersHandler activeUserHandler) : ControllerBase
{
    [HttpGet("registrations")]
    [ProducesResponseType<RegisteredUserListPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegisteredUserListPage>> Registrations(
        [FromQuery] RegistrationUserListRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await registrationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await registrationHandler.HandleAsync(
            new RegistrationListQuery(
                request.From, request.To, request.TenantId, request.Page, request.PageSize),
            cancellationToken));
    }

    [HttpGet("active")]
    [ProducesResponseType<ActiveUserListPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActiveUserListPage>> Active(
        [FromQuery] ActiveUserListRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await activeUserValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await activeUserHandler.HandleAsync(
            new ActiveUserListQuery(
                request.Date!.Value, request.TenantId, request.Page, request.PageSize),
            cancellationToken));
    }
}
