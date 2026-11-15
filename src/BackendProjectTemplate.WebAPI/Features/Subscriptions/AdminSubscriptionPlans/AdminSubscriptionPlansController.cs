using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Application.Subscriptions.Features.CreateSubscriptionPlan;
using BackendProjectTemplate.Application.Subscriptions.Features.ListAdminSubscriptionPlans;
using BackendProjectTemplate.Application.Subscriptions.Features.SetSubscriptionPlanActivation;
using BackendProjectTemplate.Application.Subscriptions.Features.UpdateSubscriptionPlan;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;

/// <summary>
/// Administrative plan catalog. Reading the full catalog (including inactive plans) requires
/// <c>subscriptions.read</c>; every mutation requires <c>subscriptions.plans.manage</c>. Plans are never deleted.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminSubscriptionPlans.Route)]
public sealed class AdminSubscriptionPlansController(
    ListAdminSubscriptionPlansHandler listAdminSubscriptionPlansHandler,
    CreateSubscriptionPlanValidator createSubscriptionPlanValidator,
    CreateSubscriptionPlanHandler createSubscriptionPlanHandler,
    UpdateSubscriptionPlanValidator updateSubscriptionPlanValidator,
    UpdateSubscriptionPlanHandler updateSubscriptionPlanHandler,
    SetSubscriptionPlanActivationHandler setSubscriptionPlanActivationHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionsRead)]
    [ProducesResponseType<IReadOnlyList<AdminSubscriptionPlanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionPlanResponse>>> List(
        [FromQuery] ListAdminSubscriptionPlansRequest request,
        CancellationToken cancellationToken) =>
        Ok(await listAdminSubscriptionPlansHandler.HandleAsync(new ListAdminSubscriptionPlansCommand(request.IsActive), cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionPlansManage)]
    [RequireStepUpMfa(StepUpMfaOperations.CreateSubscriptionPlan)]
    [ProducesResponseType<AdminSubscriptionPlanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> Create(
        [FromBody] CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createSubscriptionPlanValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await createSubscriptionPlanHandler.HandleAsync(
            new CreateSubscriptionPlanCommand(
                request.Code,
                request.Name,
                request.Price,
                request.CurrencyId,
                StrictEnumParser.Parse<BillingInterval>(request.BillingInterval),
                request.IsActive,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            CreateSubscriptionPlanStatus.Created => Created(EndpointUrl.AdminSubscriptionPlans.PlanV1(result.Plan!.Id), result.Plan),
            CreateSubscriptionPlanStatus.CodeConflict => Problem(result.Error, statusCode: StatusCodes.Status409Conflict),
            CreateSubscriptionPlanStatus.CurrencyNotFound => Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["plan"] = [result.Error!] }))
        };
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionPlansManage)]
    [RequireStepUpMfa(StepUpMfaOperations.UpdateSubscriptionPlan)]
    [ProducesResponseType<AdminSubscriptionPlanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateSubscriptionPlanValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await updateSubscriptionPlanHandler.HandleAsync(
            new UpdateSubscriptionPlanCommand(id, request.Name, request.Price, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status switch
        {
            UpdateSubscriptionPlanStatus.Updated or UpdateSubscriptionPlanStatus.Unchanged => Ok(result.Plan),
            UpdateSubscriptionPlanStatus.NotFound => Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["plan"] = [result.Error!] }))
        };
    }

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = PermissionPolicyNames.SubscriptionPlansManage)]
    [RequireStepUpMfa(StepUpMfaOperations.SetSubscriptionPlanActivation)]
    [ProducesResponseType<AdminSubscriptionPlanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> SetActivation(
        [FromRoute] Guid id,
        [FromBody] SetSubscriptionPlanActivationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setSubscriptionPlanActivationHandler.HandleAsync(
            new SetSubscriptionPlanActivationCommand(id, request.IsActive, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);
        return result.Status == SetSubscriptionPlanActivationStatus.NotFound
            ? Problem(result.Error, statusCode: StatusCodes.Status404NotFound)
            : Ok(result.Plan);
    }
}
