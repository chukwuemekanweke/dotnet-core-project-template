using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_CancellingSubscriptionsAdministratively_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task SupportImmediateAndPeriodEndCancellationWithoutDeletingHistory()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        await AuthenticateAsync();
        var now = UtcNow();
        var active = await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: now.AddDays(10)));
        var scheduled = await AllocateSuccessfullyAsync(TenantAllocation(now.AddDays(20), now.AddDays(30)));
        var stakeholderGrant = await AllocateSuccessfullyAsync(StakeholderAllocation(TargetStakeholderId));

        var periodEnd = await CancelAsync(EndpointUrl.AdminSubscriptions.CancellationAtPeriodEndV1(active.SubscriptionId), "Contract ending");
        periodEnd.Status.ShouldBe("Active");
        periodEnd.CancelAtPeriodEnd.ShouldBeTrue();
        periodEnd.EntitlementEndUtc.ShouldBe(active.EntitlementEndUtc);
        (await CancelAsync(EndpointUrl.AdminSubscriptions.CancellationAtPeriodEndV1(active.SubscriptionId), "Again")).CancellationReason.ShouldBe("Contract ending");

        var cancelledScheduled = await CancelAsync(EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(scheduled.SubscriptionId), "Promotion withdrawn");
        cancelledScheduled.Status.ShouldBe("Cancelled");
        using (var periodEndOnScheduled = await PostCancellationAsync(
                   EndpointUrl.AdminSubscriptions.CancellationAtPeriodEndV1(stakeholderGrant.SubscriptionId),
                   new AdminSubscriptionCancellationRequest(ForeignTenantId, "Wrong tenant")))
        {
            periodEndOnScheduled.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        var cancelledActive = await CancelAsync(EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(stakeholderGrant.SubscriptionId), "Abuse");
        cancelledActive.Status.ShouldBe("Cancelled");
        cancelledActive.EndedAtUtc.ShouldNotBeNull();
        (await CancelAsync(EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(stakeholderGrant.SubscriptionId), "Repeat"))
            .CancellationReason.ShouldBe("Abuse");

        var reused = await AllocateSuccessfullyAsync(TenantAllocation(now.AddDays(20), now.AddDays(25)));
        reused.Status.ShouldBe("Scheduled");

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.SubscriptionPeriods.CountAsync(item => item.SubscriptionId == stakeholderGrant.SubscriptionId)).ShouldBe(1);
        var history = await dbContext.SubscriptionStatusHistory
            .Where(item => item.SubscriptionId == stakeholderGrant.SubscriptionId)
            .OrderBy(item => item.OccurredAtUtc)
            .ToListAsync();
        history.Count.ShouldBe(2);
        history[^1].Reason.ShouldBe(SubscriptionLifecycleReasons.AdministrativeImmediateCancellation);
        history[^1].Details.ShouldBe("Abuse");
        history[^1].ActorId.ShouldBe(AdminStakeholderId.ToString());
    }

    private Task<HttpResponseMessage> PostCancellationAsync(string url, AdminSubscriptionCancellationRequest request) =>
        Client.PostAsJsonAsync(url, request);

    private async Task<AdminSubscriptionLifecycleResponse> CancelAsync(string url, string reason)
    {
        using var response = await PostCancellationAsync(url, new AdminSubscriptionCancellationRequest(TargetTenantId, reason));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AdminSubscriptionLifecycleResponse>())!;
    }
}
