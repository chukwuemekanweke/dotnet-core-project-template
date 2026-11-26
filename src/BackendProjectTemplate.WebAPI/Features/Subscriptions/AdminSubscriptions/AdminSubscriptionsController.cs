using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;
using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;
using BackendProjectTemplate.Application.Subscriptions.Features.RetrySubscriptionPurchaseFulfillment;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;

/// <summary>
/// Cross-tenant administrative subscription operations. Every action is authorized by a static permission
/// policy (which also requires an active session); target tenant and subscription are explicit request inputs
/// and the acting administrator is always taken from the authenticated actor, never from the request.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminSubscriptions.Route)]
public sealed class AdminSubscriptionsController(
    ListSubscriptionsValidator listSubscriptionsValidator,
    ListSubscriptionsHandler listSubscriptionsHandler,
    GetSubscriptionDetailValidator getSubscriptionDetailValidator,
    GetSubscriptionDetailHandler getSubscriptionDetailHandler,
    AllocateSubscriptionValidator allocateSubscriptionValidator,
    AllocateSubscriptionHandler allocateSubscriptionHandler,
    AdminSubscriptionCancellationValidator adminSubscriptionCancellationValidator,
    CancelSubscriptionImmediatelyHandler cancelSubscriptionImmediatelyHandler,
    AdministrativelyCancelSubscriptionAtPeriodEndHandler administrativelyCancelSubscriptionAtPeriodEndHandler,
    RetryPurchaseFulfillmentValidator retryPurchaseFulfillmentValidator,
    RetrySubscriptionPurchaseFulfillmentHandler retrySubscriptionPurchaseFulfillmentHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsRead)]
    [ProducesResponseType<ListSubscriptionsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListSubscriptionsResult>> List(
        [FromQuery] ListSubscriptionsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await listSubscriptionsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await listSubscriptionsHandler.HandleAsync(
            new ListSubscriptionsCommand(
                request.TenantId,
                request.BillingAccountId,
                StrictEnumParser.ParseOptional<BillingAccountOwnerType>(request.BillingAccountOwnerType),
                request.StakeholderId,
                request.PlanId,
                StrictEnumParser.ParseOptional<SubscriptionStatus>(request.Status),
                StrictEnumParser.ParseOptional<SubscriptionAllocationSource>(request.AllocationSource),
                request.AutoRenew,
                request.CancelAtPeriodEnd,
                request.CreatedFromUtc,
                request.CreatedToUtc,
                request.CurrentPeriodFromUtc,
                request.CurrentPeriodToUtc,
                request.Cursor,
                request.Limit),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsRead)]
    [ProducesResponseType<SubscriptionDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDetailResponse>> Get(
        [FromRoute] Guid id,
        [FromQuery] GetSubscriptionDetailRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await getSubscriptionDetailValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await getSubscriptionDetailHandler.HandleAsync(new GetSubscriptionDetailCommand(id, request.HistoryLimit), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("allocations")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsAllocate)]
    [RequireStepUpMfa(StepUpMfaOperations.AllocateSubscription)]
    [ProducesResponseType<AllocateSubscriptionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<AllocateSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AllocateSubscriptionResponse>> Allocate(
        [FromBody] AllocateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await allocateSubscriptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await allocateSubscriptionHandler.HandleAsync(
            new AllocateSubscriptionCommand(
                request.TenantId,
                StrictEnumParser.Parse<BillingAccountOwnerType>(request.BillingAccountOwnerType),
                request.StakeholderId,
                request.PlanId,
                request.StartAtUtc,
                request.EndAtUtc,
                StrictEnumParser.Parse<SubscriptionAllocationSource>(request.AllocationSource),
                request.Reason,
                request.IdempotencyKey,
                ActorContext.FromCurrentActor(currentActor),
                StrictEnumParser.ParseOptional<BillingInterval>(request.BillingInterval)),
            cancellationToken);

        return result.Status switch
        {
            AllocateSubscriptionStatus.Allocated => Created(
                EndpointUrl.AdminSubscriptions.DetailV1(result.Response!.SubscriptionId),
                result.Response),
            AllocateSubscriptionStatus.Replayed => Ok(result.Response),
            AllocateSubscriptionStatus.PlanNotFound or AllocateSubscriptionStatus.OwnerNotFound =>
                Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            AllocateSubscriptionStatus.InvalidPeriod => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(request.EndAtUtc)] = [result.Error!] })),
            AllocateSubscriptionStatus.BillingIntervalNotOffered => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(request.BillingInterval)] = [result.Error!] })),
            AllocateSubscriptionStatus.PlanInactive or AllocateSubscriptionStatus.OwnerTenantMismatch =>
                Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Problem(result.Error, statusCode: StatusCodes.Status409Conflict)
        };
    }

    [HttpPost("{id:guid}/immediate-cancellation")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsManage)]
    [RequireStepUpMfa(StepUpMfaOperations.CancelSubscriptionImmediately)]
    [ProducesResponseType<AdminSubscriptionLifecycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminSubscriptionLifecycleResponse>> CancelImmediately(
        [FromRoute] Guid id,
        [FromBody] AdminSubscriptionCancellationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await adminSubscriptionCancellationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await cancelSubscriptionImmediatelyHandler.HandleAsync(
            new CancelSubscriptionImmediatelyCommand(id, request.TenantId, request.Reason, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            CancelSubscriptionImmediatelyStatus.Cancelled or CancelSubscriptionImmediatelyStatus.AlreadyCancelled => Ok(result.Subscription),
            CancelSubscriptionImmediatelyStatus.NotFound => Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(result.Error, statusCode: StatusCodes.Status409Conflict)
        };
    }

    [HttpPost("{id:guid}/cancellation-at-period-end")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsManage)]
    [RequireStepUpMfa(StepUpMfaOperations.CancelSubscriptionAtPeriodEnd)]
    [ProducesResponseType<AdminSubscriptionLifecycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminSubscriptionLifecycleResponse>> CancelAtPeriodEnd(
        [FromRoute] Guid id,
        [FromBody] AdminSubscriptionCancellationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await adminSubscriptionCancellationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await administrativelyCancelSubscriptionAtPeriodEndHandler.HandleAsync(
            new AdministrativelyCancelSubscriptionAtPeriodEndCommand(id, request.TenantId, request.Reason, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            AdministrativelyCancelSubscriptionAtPeriodEndStatus.Scheduled or
                AdministrativelyCancelSubscriptionAtPeriodEndStatus.AlreadyScheduled => Ok(result.Subscription),
            AdministrativelyCancelSubscriptionAtPeriodEndStatus.NotFound => Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(result.Error, statusCode: StatusCodes.Status409Conflict)
        };
    }

    [HttpPost("{id:guid}/purchase-fulfillment-retries")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsManage)]
    [RequireStepUpMfa(StepUpMfaOperations.RetrySubscriptionPurchaseFulfillment)]
    [ProducesResponseType<RetrySubscriptionPurchaseFulfillmentResult>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RetrySubscriptionPurchaseFulfillmentResult>> RetryPurchaseFulfillment(
        [FromRoute] Guid id,
        [FromBody] RetryPurchaseFulfillmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await retryPurchaseFulfillmentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await retrySubscriptionPurchaseFulfillmentHandler.HandleAsync(
            new RetrySubscriptionPurchaseFulfillmentCommand(
                id,
                request.TenantId,
                ActorContext.FromCurrentActor(currentActor),
                request.Reason),
            cancellationToken);
        return result.Status switch
        {
            RetrySubscriptionPurchaseFulfillmentStatus.Requested => Accepted(result),
            RetrySubscriptionPurchaseFulfillmentStatus.NotFound => Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(result.Error, statusCode: StatusCodes.Status409Conflict)
        };
    }
}
