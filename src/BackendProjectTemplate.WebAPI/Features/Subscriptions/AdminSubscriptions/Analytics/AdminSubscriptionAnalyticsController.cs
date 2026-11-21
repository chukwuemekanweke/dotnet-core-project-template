using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsPlans;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsRenewals;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsStatuses;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsSummary;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsTimeline;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions.Analytics;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminSubscriptionAnalytics.Route)]
[Authorize(Policy = PermissionPolicyNames.SubscriptionAnalyticsRead)]
public sealed class AdminSubscriptionAnalyticsController(
    AdminSubscriptionAnalyticsValidator validator,
    AdminSubscriptionAnalyticsTimelineValidator timelineValidator,
    GetAdminSubscriptionAnalyticsSummaryHandler summaryHandler,
    GetAdminSubscriptionAnalyticsTimelineHandler timelineHandler,
    GetAdminSubscriptionAnalyticsRenewalsHandler renewalsHandler,
    GetAdminSubscriptionAnalyticsPlansHandler plansHandler,
    GetAdminSubscriptionAnalyticsStatusesHandler statusesHandler) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<SubscriptionAnalyticsSummaryResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SubscriptionAnalyticsSummaryResponse>> Summary(
        [FromQuery] AdminSubscriptionAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, summaryHandler.HandleAsync, cancellationToken);

    [HttpGet("timeline")]
    [ProducesResponseType<SubscriptionAnalyticsTimelineResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionAnalyticsTimelineResponse>> Timeline(
        [FromQuery] AdminSubscriptionAnalyticsTimelineRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await timelineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));

        return Ok(await timelineHandler.HandleAsync(
            ToQuery(request),
            StrictEnumParser.Parse<Interval>(request.Interval),
            cancellationToken));
    }

    [HttpGet("renewals")]
    [ProducesResponseType<SubscriptionAnalyticsRenewalsResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SubscriptionAnalyticsRenewalsResponse>> Renewals(
        [FromQuery] AdminSubscriptionAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, renewalsHandler.HandleAsync, cancellationToken);

    [HttpGet("plans")]
    [ProducesResponseType<SubscriptionAnalyticsPlansResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SubscriptionAnalyticsPlansResponse>> Plans(
        [FromQuery] AdminSubscriptionAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, plansHandler.HandleAsync, cancellationToken);

    [HttpGet("statuses")]
    [ProducesResponseType<SubscriptionAnalyticsStatusesResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<SubscriptionAnalyticsStatusesResponse>> Statuses(
        [FromQuery] AdminSubscriptionAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, statusesHandler.HandleAsync, cancellationToken);

    private async Task<ActionResult<T>> ExecuteAsync<T>(
        AdminSubscriptionAnalyticsRequest request,
        Func<SubscriptionAnalyticsQuery, CancellationToken, Task<T>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));

        return Ok(await handler(ToQuery(request), cancellationToken));
    }

    private static SubscriptionAnalyticsQuery ToQuery(AdminSubscriptionAnalyticsRequest request) => new(
        request.From,
        request.To,
        request.PlanId,
        StrictEnumParser.ParseOptional<BillingAccountOwnerType>(request.OwnerType),
        request.TenantId,
        request.CurrencyId);
}
