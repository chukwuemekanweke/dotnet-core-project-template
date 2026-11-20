using BackendProjectTemplate.Application.Analytics;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;

namespace BackendProjectTemplate.WebAPI.Infrastructure;

public sealed class UserActivityMiddleware(
    RequestDelegate next,
    ILogger<UserActivityMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentActor currentActor,
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        UserActivityObservationService observationService,
        TimeProvider timeProvider)
    {
        await next(context);

        if (!Qualifies(context)
            || !Guid.TryParse(currentActor.ActorId, out var stakeholderId)
            || !currentActor.TenantId.HasValue)
        {
            return;
        }

        try
        {
            var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(
                stakeholderId,
                context.RequestAborted);
            if (stakeholder is null || stakeholder.TenantId != currentActor.TenantId.Value)
            {
                logger.LogWarning(
                    "Skipped user activity because authenticated stakeholder {StakeholderId} did not match tenant {TenantId}.",
                    stakeholderId,
                    currentActor.TenantId.Value);
                return;
            }

            await observationService.ObserveAsync(
                new UserActivityObservation(
                    stakeholder.TenantId,
                    stakeholder.StakeholderId,
                    timeProvider.GetUtcNow(),
                    UserActivityType.AuthenticatedRequest),
                context.RequestAborted);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Failed to submit an authenticated user activity observation.");
        }
    }

    private static bool Qualifies(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || context.Response.StatusCode is < StatusCodes.Status200OK or >= StatusCodes.Status300MultipleChoices
            || context.GetEndpoint()?.Metadata.GetMetadata<ExcludeFromUserActivityAttribute>() is not null)
        {
            return false;
        }

        var path = context.Request.Path;
        return !path.StartsWithSegments("/health")
            && !path.StartsWithSegments("/metrics")
            && !path.StartsWithSegments("/openapi")
            && !path.StartsWithSegments("/scalar")
            && !path.StartsWithSegments("/api/v1/payments/webhooks")
            && !path.StartsWithSegments("/api/v1/email-notifications/webhooks")
            && !path.StartsWithSegments("/api/v1/telemetry")
            && path != "/";
    }
}
