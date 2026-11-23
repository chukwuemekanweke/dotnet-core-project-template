using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Features.ListEligiblePaymentProviders;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Plans;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.SubscriptionPlans.Route)]
public sealed class SubscriptionPlansController(
    ListSubscriptionPlansHandler handler,
    ListEligiblePaymentProvidersHandler paymentProvidersHandler,
    SubscriptionOwnerValidator ownerValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ListSubscriptionPlansResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ListSubscriptionPlansResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    [HttpGet("{id:guid}/payment-providers")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
    [ProducesResponseType<IReadOnlyList<EligiblePaymentProviderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EligiblePaymentProviderResponse>>> ListPaymentProviders(
        [FromRoute] Guid id,
        [FromQuery] SubscriptionOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ownerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        _ = Enum.TryParse<BillingAccountOwnerType>(request.BillingAccountOwnerType, true, out var ownerType);
        var result = await paymentProvidersHandler.HandleAsync(
            new ListEligiblePaymentProvidersCommand(id, ownerType, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
