using System.Diagnostics;
using System.Security.Claims;
using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.WebAPI.Infrastructure;

public sealed class CurrentActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentActorAccessor currentActorAccessor)
    {
        var actorId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? (context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null)
            ?? "anonymous";

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
}
