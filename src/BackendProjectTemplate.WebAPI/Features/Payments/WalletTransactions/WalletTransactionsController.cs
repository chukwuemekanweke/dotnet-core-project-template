using Asp.Versioning;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Payments.WalletTransactions;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Payments.Route)]
public sealed class WalletTransactionsController(
    GetStakeholderWalletTransactionsHandler handler,
    GetStakeholderWalletTransactionsValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("wallet-transactions")]
    [ProducesResponseType<GetStakeholderWalletTransactionsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetStakeholderWalletTransactionsResult>> Handle(
        [FromQuery] GetStakeholderWalletTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new GetStakeholderWalletTransactionsCommand(
                request.Limit,
                request.Cursor,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return Ok(result);
    }
}
