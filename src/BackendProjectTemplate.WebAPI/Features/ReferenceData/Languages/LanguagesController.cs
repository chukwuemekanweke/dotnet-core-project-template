using Asp.Versioning;
using BackendProjectTemplate.Application.ReferenceData.Features.GetLanguages;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.ReferenceData.Languages;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Languages.Route)]
public sealed class LanguagesController(GetLanguagesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetLanguagesResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<GetLanguagesResponse>> Get() => Ok(handler.Handle());
}
