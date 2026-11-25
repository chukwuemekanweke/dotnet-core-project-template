using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Infrastructure.StepUpMfa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.Providers;

/// <summary>
/// Payment provider administration. Permissions are applied per action because the controller mixes
/// read (<c>payments.read</c>) and step-up-protected management (<c>payments.manage</c>) operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.PaymentProviders.Route)]
public sealed class PaymentProvidersController(
    ActivatePaymentProviderHandler handler,
    ListPaymentProvidersHandler listHandler) : ControllerBase
{
    /// <summary>
    /// Lists every payment provider, active or inactive, ordered by name. <c>isActive</c> is the
    /// administrative activation state, not provider health. Credentials and configuration are never returned.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicyNames.PaymentsRead)]
    [ProducesResponseType<IReadOnlyList<PaymentProviderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PaymentProviderResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await listHandler.HandleAsync(cancellationToken));

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = PermissionPolicyNames.PaymentsManage)]
    [RequireStepUpMfa(StepUpMfaOperations.SetPaymentProviderActivation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActivation(
        [FromRoute] Guid id,
        [FromBody] SetPaymentProviderActivationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ActivatePaymentProviderCommand(id, request.IsActive),
            cancellationToken);

        return result.Status switch
        {
            ActivatePaymentProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Payment provider not found."),
            _ => NoContent()
        };
    }
}
