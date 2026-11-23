using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;
using BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionPurchaseStatus;
using BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions.Exceptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Subscriptions.Route)]
public sealed class SubscriptionsController(
    PurchaseSubscriptionHandler purchaseHandler,
    GetCurrentSubscriptionHandler currentHandler,
    GetSubscriptionPurchaseStatusHandler purchaseStatusHandler,
    GetBillingHistoryHandler billingHistoryHandler,
    CancelSubscriptionAtPeriodEndHandler cancellationHandler,
    PurchaseSubscriptionValidator purchaseValidator,
    SubscriptionOwnerValidator ownerValidator,
    BillingHistoryValidator billingHistoryValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("purchases")]
    [ProducesResponseType<PurchaseSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseSubscriptionResponse>> Purchase(
        [FromBody] PurchaseSubscriptionRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var validation = await purchaseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        if (idempotencyKey == Guid.Empty)
        {
            ModelState.AddModelError("Idempotency-Key", "A non-empty Idempotency-Key header is required.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        PurchaseSubscriptionResult result;
        try
        {
            result = await purchaseHandler.HandleAsync(
                new PurchaseSubscriptionCommand(
                    request.PlanId,
                    request.PaymentProviderId,
                    ownerType,
                    request.AutoRenew,
                    idempotencyKey,
                    ActorContext.FromCurrentActor(currentActor)),
                cancellationToken);
        }
        catch (SubscriptionEntitlementConflictException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (SubscriptionPurchaseIdempotencyKeyReusedException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(new PurchaseSubscriptionResponse(
            result.SubscriptionId,
            result.PurchaseId,
            result.MerchantReference,
            result.PaymentStatus.ToString(),
            result.PaymentProviderId,
            result.PaymentProviderName,
            result.ExpiresAtUtc,
            result.PaymentMethodType.ToString(),
            result.InstructionFields));
    }

    [HttpGet("purchases/{purchaseId:guid}")]
    [ProducesResponseType<GetSubscriptionPurchaseStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetSubscriptionPurchaseStatusResponse>> GetPurchaseStatus(
        [FromRoute] Guid purchaseId,
        [FromQuery] SubscriptionOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ownerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        var result = await purchaseStatusHandler.HandleAsync(
            new GetSubscriptionPurchaseStatusCommand(
                purchaseId,
                ownerType,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("billing-history")]
    [ProducesResponseType<GetBillingHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetBillingHistoryResponse>> GetBillingHistory(
        [FromQuery] BillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await billingHistoryValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        return Ok(await billingHistoryHandler.HandleAsync(
            new GetBillingHistoryCommand(
                ownerType,
                request.Page,
                request.PageSize,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken));
    }

    [HttpGet("current")]
    [ProducesResponseType<GetCurrentSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCurrentSubscriptionResponse>> GetCurrent(
        [FromQuery] SubscriptionOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ownerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        var result = await currentHandler.HandleAsync(
            new GetCurrentSubscriptionCommand(ownerType, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("current/cancellation-at-period-end")]
    [ProducesResponseType<CancelSubscriptionAtPeriodEndResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancelSubscriptionAtPeriodEndResult>> ScheduleCancellation(
        [FromBody] SubscriptionOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ownerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        var result = await cancellationHandler.HandleAsync(
            new CancelSubscriptionAtPeriodEndCommand(ownerType, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Found ? Ok(result) : NotFound();
    }
}
