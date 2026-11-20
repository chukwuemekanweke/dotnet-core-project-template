using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace BackendProjectTemplate.Application.Analytics;

public sealed class UserActivityObservationService(
    IUserActivityThrottle throttle,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    ILogger<UserActivityObservationService> logger)
{
    private static readonly TimeSpan ClaimTtl = TimeSpan.FromMinutes(10);
    private const long WindowSeconds = 300;

    public async Task ObserveAsync(
        UserActivityObservation observation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observation.ActivityType);
        if (observation.TenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier is required.", nameof(observation));
        }

        if (observation.StakeholderId == Guid.Empty)
        {
            throw new ArgumentException("A stakeholder identifier is required.", nameof(observation));
        }

        var observationId = Guid.CreateVersion7();
        var occurredAtUtc = observation.OccurredAtUtc.ToUniversalTime();
        var activityDate = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime);
        var windowStartUnixSeconds = occurredAtUtc.ToUnixTimeSeconds() / WindowSeconds * WindowSeconds;
        var claimKey = FormattableString.Invariant(
            $"user-activity:v1:{observation.TenantId:N}:{observation.StakeholderId:N}:{activityDate:yyyy-MM-dd}:{windowStartUnixSeconds}");

        try
        {
            if (!await throttle.TryClaimAsync(claimKey, observationId, ClaimTtl, cancellationToken))
            {
                return;
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "User activity throttling is unavailable; publishing observation {ObservationId} without throttling.",
                observationId);
        }

        try
        {
            await eventPublisher.PublishAsync(new UserActivityObserved(activityDate, observation.ActivityType)
            {
                MessageId = observationId,
                TenantId = observation.TenantId,
                StakeholderId = observation.StakeholderId,
                OccuredAt = occurredAtUtc
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryReleaseClaimAsync(claimKey, observationId);
            throw;
        }
    }

    private async Task TryReleaseClaimAsync(
        string claimKey,
        Guid observationId)
    {
        try
        {
            await throttle.ReleaseAsync(claimKey, observationId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to release user activity claim for observation {ObservationId} after outbox persistence failed.",
                observationId);
        }
    }
}
