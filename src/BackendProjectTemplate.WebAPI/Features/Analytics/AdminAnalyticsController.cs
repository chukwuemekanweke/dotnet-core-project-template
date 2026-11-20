using Asp.Versioning;
using BackendProjectTemplate.Application.Analytics.Features;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Analytics;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/analytics")]
[Authorize(Policy = PermissionPolicyNames.UserAnalyticsRead)]
[ExcludeFromUserActivity]
public sealed class AdminAnalyticsController(
    RegistrationAnalyticsValidator registrationValidator,
    ActiveUserAnalyticsValidator activeUserValidator,
    GetRegistrationAnalyticsHandler registrationHandler,
    GetActiveUserAnalyticsHandler activeUserHandler) : ControllerBase
{
    [HttpGet("registrations")]
    [ProducesResponseType<RegistrationAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationAnalyticsResponse>> Registrations(
        [FromQuery] RegistrationAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await registrationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await registrationHandler.HandleAsync(
            new RegistrationAnalyticsQuery(
                request.From,
                request.To,
                StrictEnumParser.Parse<Interval>(request.Interval),
                request.TenantId),
            cancellationToken));
    }

    [HttpGet("active-users")]
    [ProducesResponseType<ActiveUserAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActiveUserAnalyticsResponse>> ActiveUsers(
        [FromQuery] ActiveUserAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await activeUserValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await activeUserHandler.HandleAsync(
            new ActiveUserAnalyticsQuery(
                request.From, request.To, request.TenantId),
            cancellationToken));
    }
}
