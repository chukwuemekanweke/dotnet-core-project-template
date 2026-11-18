using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Subscriptions;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.AdminPayments;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.AdminPayments.Route)]
[Authorize(Policy = PermissionPolicyNames.PaymentsRead)]
public sealed class AdminPaymentsController(
    ListAdminPaymentsValidator listValidator,
    GetAdminPaymentsHandler listHandler,
    GetAdminPaymentDetailValidator detailValidator,
    GetAdminPaymentDetailHandler detailHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<GetAdminPaymentsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GetAdminPaymentsResult>> List(
        [FromQuery] ListAdminPaymentsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        return Ok(await listHandler.HandleAsync(
            new GetAdminPaymentsCommand(
                request.TenantId,
                request.StakeholderId,
                request.PaymentProviderId,
                request.CurrencyId,
                StrictEnumParser.ParseOptional<PaymentIntent>(request.PaymentIntent),
                StrictEnumParser.ParseOptional<PaymentStatus>(request.PaymentStatus),
                StrictEnumParser.ParseOptional<PaymentMethodType>(request.PaymentMethodType),
                request.CreatedFromUtc,
                request.CreatedToUtc,
                request.Search,
                request.Cursor,
                request.Limit),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetAdminPaymentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetAdminPaymentDetailResponse>> Get(
        [FromRoute] Guid id,
        [FromQuery] GetAdminPaymentDetailRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(id)] = ["Payment transaction id must not be empty."] }));
        }

        var validation = await detailValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await detailHandler.HandleAsync(
            new GetAdminPaymentDetailCommand(id, request.WebhookHistoryLimit),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
