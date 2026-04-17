using System.Diagnostics;
using System.Security.Claims;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

namespace BackendProjectTemplate.WebAPI.Infrastructure;

public sealed class CurrentActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentActorAccessor currentActorAccessor,
        IStakeholderReadModelRepository stakeholderReadModelRepository)
    {
        var actorId = await ResolveActorIdAsync(context, stakeholderReadModelRepository);

        Guid? tenantId = null;
        if (Guid.TryParse(context.Request.Headers["X-Tenant-Id"], out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }

        var correlationId = context.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N");
        }

        currentActorAccessor.Set(actorId, tenantId, correlationId);
        await next(context);
    }

    private static async Task<string> ResolveActorIdAsync(
        HttpContext context,
        IStakeholderReadModelRepository stakeholderReadModelRepository)
    {
        var stakeholderId = context.User.FindFirstValue(CustomClaimTypes.StakeholderId);
        if (Guid.TryParse(stakeholderId, out var parsedStakeholderId))
        {
            return parsedStakeholderId.ToString();
        }

        var appUserId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (Guid.TryParse(appUserId, out var parsedAppUserId))
        {
            var stakeholder = await stakeholderReadModelRepository.GetByAppUserIdAsync(parsedAppUserId, context.RequestAborted);
            if (stakeholder is not null)
            {
                return stakeholder.StakeholderId.ToString();
            }
        }

        return (context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null)
            ?? "anonymous";
    }
}
