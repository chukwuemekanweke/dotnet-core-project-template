using Asp.Versioning;
using BackendProjectTemplate.Application.Kyc.Features.GetCurrentVerification;
using BackendProjectTemplate.Application.Kyc.Features.StartVerification;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Kyc;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Kyc.Route)]
public sealed class KycController(
    StartKycVerificationHandler startHandler,
    GetCurrentKycVerificationHandler getHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("start")]
    [ProducesResponseType<StartKycVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<StartKycVerificationResponse>> Start(CancellationToken cancellationToken)
    {
        var result = await startHandler.HandleAsync(new(ActorContext.FromCurrentActor(currentActor)), cancellationToken);
        return result.Status switch
        {
            StartKycVerificationStatus.Success => Ok(result.Verification),
            StartKycVerificationStatus.NotAuthenticated => Unauthorized(),
            StartKycVerificationStatus.StakeholderNotFound => NotFound(),
            StartKycVerificationStatus.AlreadyVerified => Conflict("The stakeholder is already verified."),
            StartKycVerificationStatus.IncompleteProfile => BadRequest(result.Error),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, "The verification provider is temporarily unavailable.")
        };
    }

    [HttpGet]
    [ProducesResponseType<GetCurrentKycVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetCurrentKycVerificationResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new(ActorContext.FromCurrentActor(currentActor)), cancellationToken);
        return result.IsAuthenticated ? Ok(result.Verification) : Unauthorized();
    }
}
