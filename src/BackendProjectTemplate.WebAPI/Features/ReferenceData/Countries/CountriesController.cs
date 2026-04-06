using Asp.Versioning;
using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.ReferenceData.Countries;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Countries.Route)]
public sealed class CountriesController(GetCountriesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetCountriesResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetCountriesResponse>>> Handle(CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(cancellationToken);
        return Ok(response);
    }
}
