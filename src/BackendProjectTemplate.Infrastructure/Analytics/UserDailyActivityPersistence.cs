using BackendProjectTemplate.Domain.Analytics;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace BackendProjectTemplate.Infrastructure.Analytics;

internal sealed class UserDailyActivityPersistence(
    DbContextOptions<AppDbContext> dbContextOptions) : IUserDailyActivityPersistence
{
    private const int MaximumAttempts = 12;
    private const int RetryDelayMilliseconds = 25;

    public async Task ObserveAsync(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc,
        string activityType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityType);
        occurredAtUtc = occurredAtUtc.ToUniversalTime();

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                await ObserveOnceAsync(
                    observationId,
                    tenantId,
                    stakeholderId,
                    activityDate,
                    occurredAtUtc,
                    activityType,
                    cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < MaximumAttempts && IsRetryable(exception))
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(RetryDelayMilliseconds * attempt),
                    cancellationToken);
            }
        }
    }

    private async Task ObserveOnceAsync(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc,
        string activityType,
        CancellationToken cancellationToken)
    {
        await using var dbContext = new AppDbContext(dbContextOptions);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var alreadyObserved = await dbContext.UserActivityObservationReceipts
            .AsNoTracking()
            .AnyAsync(receipt => receipt.ObservationId == observationId, cancellationToken);
        if (alreadyObserved)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        dbContext.UserActivityObservationReceipts.Add(UserActivityObservationReceipt.Create(
            observationId,
            tenantId,
            stakeholderId,
            activityDate,
            occurredAtUtc,
            activityType));

        var dailyActivity = await dbContext.UserDailyActivities.SingleOrDefaultAsync(
            activity => activity.TenantId == tenantId
                && activity.StakeholderId == stakeholderId
                && activity.ActivityDate == activityDate,
            cancellationToken);
        if (dailyActivity is null)
        {
            dbContext.UserDailyActivities.Add(UserDailyActivity.Create(
                tenantId,
                stakeholderId,
                activityDate,
                occurredAtUtc));
        }
        else
        {
            dailyActivity.Observe(occurredAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsRetryable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException
                && postgresException.SqlState is PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }
        }

        return false;
    }
}
