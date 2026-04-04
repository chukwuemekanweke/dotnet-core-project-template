using BackendProjectTemplate.Application.ReferenceData.Features.GetCountries;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.ReferenceData.GetCountries;

[ApiController]
[Route("api/reference-data/countries")]
public sealed class GetCountriesController(GetCountriesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetCountriesResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetCountriesResponse>>> Handle(CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(cancellationToken);
        return Ok(response);
    }
}
