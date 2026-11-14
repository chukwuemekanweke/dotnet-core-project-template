using Asp.Versioning;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptionPlans;
using BackendProjectTemplate.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendProjectTemplate.WebAPI.Features.Subscriptions.Plans;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.SubscriptionPlans.Route)]
public sealed class SubscriptionPlansController(ListSubscriptionPlansHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ListSubscriptionPlansResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ListSubscriptionPlansResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));
}
