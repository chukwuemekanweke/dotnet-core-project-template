using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsIntents;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsProviders;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsStatuses;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsSummary;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentAnalyticsTimeline;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments.Analytics;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminPaymentAnalytics.Route)]
[Authorize(Policy = PermissionPolicyNames.PaymentAnalyticsRead)]
public sealed class AdminPaymentAnalyticsController(
    AdminPaymentAnalyticsValidator validator,
    AdminPaymentAnalyticsTimelineValidator timelineValidator,
    GetAdminPaymentAnalyticsSummaryHandler summaryHandler,
    GetAdminPaymentAnalyticsTimelineHandler timelineHandler,
    GetAdminPaymentAnalyticsStatusesHandler statusesHandler,
    GetAdminPaymentAnalyticsProvidersHandler providersHandler,
    GetAdminPaymentAnalyticsIntentsHandler intentsHandler) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<PaymentAnalyticsSummaryResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<PaymentAnalyticsSummaryResponse>> Summary(
        [FromQuery] AdminPaymentAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, summaryHandler.HandleAsync, cancellationToken);

    [HttpGet("timeline")]
    [ProducesResponseType<PaymentAnalyticsTimelineResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentAnalyticsTimelineResponse>> Timeline(
        [FromQuery] AdminPaymentAnalyticsTimelineRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await timelineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        return Ok(await timelineHandler.HandleAsync(
            ToQuery(request), StrictEnumParser.Parse<Interval>(request.Interval), cancellationToken));
    }

    [HttpGet("statuses")]
    [ProducesResponseType<PaymentAnalyticsStatusesResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<PaymentAnalyticsStatusesResponse>> Statuses(
        [FromQuery] AdminPaymentAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, statusesHandler.HandleAsync, cancellationToken);

    [HttpGet("providers")]
    [ProducesResponseType<PaymentAnalyticsProvidersResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<PaymentAnalyticsProvidersResponse>> Providers(
        [FromQuery] AdminPaymentAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, providersHandler.HandleAsync, cancellationToken);

    [HttpGet("intents")]
    [ProducesResponseType<PaymentAnalyticsIntentsResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<PaymentAnalyticsIntentsResponse>> Intents(
        [FromQuery] AdminPaymentAnalyticsRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(request, intentsHandler.HandleAsync, cancellationToken);

    private async Task<ActionResult<T>> ExecuteAsync<T>(
        AdminPaymentAnalyticsRequest request,
        Func<PaymentAnalyticsQuery, CancellationToken, Task<T>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        return Ok(await handler(ToQuery(request), cancellationToken));
    }

    private static PaymentAnalyticsQuery ToQuery(AdminPaymentAnalyticsRequest request) => new(
        request.From,
        request.To,
        request.PaymentProviderId,
        request.CurrencyId,
        StrictEnumParser.ParseOptional<PaymentIntent>(request.PaymentIntent),
        StrictEnumParser.ParseOptional<PaymentStatus>(request.PaymentStatus),
        request.TenantId);
}
