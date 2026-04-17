using Asp.Versioning;
using BackendProjectTemplate.Application.Providers.Features.ActivateProvider;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Providers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route(EndpointUrl.Providers.Route)]
public sealed class ProvidersController(ActivateProviderHandler activateProviderHandler) : ControllerBase
{
    [HttpPut("active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateProvider(
        [FromBody] ActivateProviderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProviderType>(request.ProviderType, true, out var providerType))
        {
            return BadRequest("ProviderType must be one of: Email, FileStorage.");
        }

        var result = await activateProviderHandler.HandleAsync(
            new ActivateProviderCommand(providerType, request.ProviderKey),
            cancellationToken);

        return result.Status switch
        {
            ActivateProviderStatus.ProviderNotFound => NotFound(result.Error ?? "Provider not found."),
            _ => NoContent()
        };
    }
}
