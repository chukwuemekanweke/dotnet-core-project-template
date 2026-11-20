using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Analytics;

[Collection(nameof(ContainersCollection))]
public sealed class When_ObservingUserActivityConcurrently_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _otherTenantId = Guid.CreateVersion7();
    private readonly Guid _stakeholderId = Guid.CreateVersion7();
    private readonly Guid _otherStakeholderId = Guid.CreateVersion7();
    private readonly DateOnly _activityDate = new(2026, 11, 20);

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.UserActivityObservationReceipts
            .Where(receipt => receipt.TenantId == _tenantId || receipt.TenantId == _otherTenantId)
            .ExecuteDeleteAsync();
        await scope.DbContext.UserDailyActivities
            .Where(activity => activity.TenantId == _tenantId || activity.TenantId == _otherTenantId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task PersistOneDailyRowPerTenantUserDateWithoutDuplicateCounts()
    {
        var firstTimestamp = new DateTimeOffset(2026, 11, 20, 23, 59, 0, TimeSpan.Zero);
        var lastTimestamp = new DateTimeOffset(2026, 11, 20, 0, 1, 0, TimeSpan.Zero);
        var observationIds = Enumerable.Range(0, 10).Select(_ => Guid.CreateVersion7()).ToArray();
        var observations = observationIds
            .Select((id, index) => ObserveAsync(
                id,
                _tenantId,
                _stakeholderId,
                index == 0 ? firstTimestamp : index == 9 ? lastTimestamp : lastTimestamp.AddMinutes(index),
                UserActivityType.AuthenticatedRequest))
            .ToList();
        observations.Add(ObserveAsync(
            observationIds[0],
            _tenantId,
            _stakeholderId,
            firstTimestamp,
            UserActivityType.AuthenticatedRequest));
        observations.Add(ObserveAsync(
            Guid.CreateVersion7(),
            _tenantId,
            _otherStakeholderId,
            lastTimestamp,
            UserActivityType.AuthenticatedRequest));
        observations.Add(ObserveAsync(
            Guid.CreateVersion7(),
            _otherTenantId,
            _stakeholderId,
            lastTimestamp,
            UserActivityType.AuthenticatedRequest));

        await Task.WhenAll(observations);

        using var assertionScope = CreateDbContextScope();
        var dailyRows = await assertionScope.DbContext.UserDailyActivities
            .Where(activity => activity.TenantId == _tenantId || activity.TenantId == _otherTenantId)
            .ToListAsync();
        dailyRows.Count.ShouldBe(3);
        var primary = dailyRows.Single(activity =>
            activity.TenantId == _tenantId && activity.StakeholderId == _stakeholderId);
        primary.ActivityCount.ShouldBe(10);
        primary.FirstActivityAtUtc.ShouldBe(lastTimestamp);
        primary.LastActivityAtUtc.ShouldBe(firstTimestamp);
    }

    [Fact]
    public async Task ClaimAndReleaseOnlyWithMatchingObservationId()
    {
        using var scope = CreateScope();
        var throttle = scope.ServiceProvider.GetRequiredService<IUserActivityThrottle>();
        var key = $"user-activity:test:{Guid.CreateVersion7():N}";
        var owner = Guid.CreateVersion7();

        (await throttle.TryClaimAsync(key, owner, TimeSpan.FromMinutes(1), CancellationToken.None)).ShouldBeTrue();
        (await throttle.TryClaimAsync(
            key,
            Guid.CreateVersion7(),
            TimeSpan.FromMinutes(1),
            CancellationToken.None)).ShouldBeFalse();

        await throttle.ReleaseAsync(key, Guid.CreateVersion7(), CancellationToken.None);
        (await throttle.TryClaimAsync(
            key,
            Guid.CreateVersion7(),
            TimeSpan.FromMinutes(1),
            CancellationToken.None)).ShouldBeFalse();

        await throttle.ReleaseAsync(key, owner, CancellationToken.None);
        var nextOwner = Guid.CreateVersion7();
        (await throttle.TryClaimAsync(
            key,
            nextOwner,
            TimeSpan.FromMinutes(1),
            CancellationToken.None)).ShouldBeTrue();
        await throttle.ReleaseAsync(key, nextOwner, CancellationToken.None);
    }

    private async Task ObserveAsync(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateTimeOffset occurredAtUtc,
        string activityType)
    {
        using var scope = CreateScope();
        var persistence = scope.ServiceProvider.GetRequiredService<IUserDailyActivityPersistence>();
        await persistence.ObserveAsync(
            observationId,
            tenantId,
            stakeholderId,
            _activityDate,
            occurredAtUtc,
            activityType,
            CancellationToken.None);
    }
}
