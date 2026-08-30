using Asp.Versioning;
using BackendProjectTemplate.Application.Stakeholders.Features.GetPreferences;
using BackendProjectTemplate.Application.Stakeholders.Features.UpdatePreferences;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.Preferences;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route($"{EndpointUrl.Stakeholders.Route}/me/preferences")]
public sealed class PreferencesController(
    GetPreferencesHandler getPreferencesHandler,
    UpdatePreferencesHandler updatePreferencesHandler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<GetPreferencesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPreferencesResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await getPreferencesHandler.HandleAsync(
            new GetPreferencesQuery(ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetPreferencesStatus.Success => Ok(result.Preferences),
            GetPreferencesStatus.NotAuthenticated => Unauthorized(),
            _ => NotFound()
        };
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updatePreferencesHandler.HandleAsync(
            new UpdatePreferencesCommand(request.Theme, request.Language, ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            UpdatePreferencesStatus.Success => NoContent(),
            UpdatePreferencesStatus.NotAuthenticated => Unauthorized(),
            UpdatePreferencesStatus.StakeholderNotFound => NotFound(),
            _ => BadRequest("Theme or language is not supported.")
        };
    }
}
