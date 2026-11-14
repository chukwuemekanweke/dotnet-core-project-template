using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.GetCurrentSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
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
    CancelSubscriptionAtPeriodEndHandler cancellationHandler,
    PurchaseSubscriptionValidator purchaseValidator,
    SubscriptionOwnerValidator ownerValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("purchases")]
    [ProducesResponseType<PurchaseSubscriptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PurchaseSubscriptionResponse>> Purchase(
        [FromBody] PurchaseSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await purchaseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        var result = await purchaseHandler.HandleAsync(
            new PurchaseSubscriptionCommand(
                request.PlanId,
                request.PaymentProviderId,
                ownerType,
                request.AutoRenew,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
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
