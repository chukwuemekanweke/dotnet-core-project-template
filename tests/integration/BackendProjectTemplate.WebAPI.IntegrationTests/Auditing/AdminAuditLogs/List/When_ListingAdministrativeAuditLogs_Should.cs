using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Auditing.ReadModels;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Preferences;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Auditing.AdminAuditLogs.List;

/// <summary>
/// Performs real administrative mutations through the API, then reads the unified administrative audit journal
/// back through its list endpoint: recording, no-op/replay/failure suppression, filters, paging, ordering,
/// historical snapshots, tenant semantics and authorization.
/// </summary>
[Collection(nameof(ContainersCollection))]
public sealed class When_ListingAdministrativeAuditLogs_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task RecordSuccessfulAdministrativeMutationsAndServeFilteredHistory()
    {
        await AuthenticateAsync();
        await GrantRoleAsync(ApplicationRoles.FinanceOfficer);
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminAuditLogs.V1))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await RevokeRoleAsync(ApplicationRoles.FinanceOfficer);
        await GrantRoleAsync(ApplicationRoles.Administrator);

        // Plan lifecycle: create, update, an identical (no-op) update, deactivate.
        var planCode = $"{PlanCodePrefix}_AUDIT";
        AdminSubscriptionPlanResponse plan;
        using (var created = await Client.PostAsJsonAsync(
            EndpointUrl.AdminSubscriptionPlans.V1,
            new CreateSubscriptionPlanRequest(planCode, "Audit Original", [new("Monthly", 5000m)], CurrencyId, Reason: "Launch audit plan")))
        {
            created.StatusCode.ShouldBe(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
            plan = (await created.Content.ReadFromJsonAsync<AdminSubscriptionPlanResponse>())!;
        }

        var update = new UpdateSubscriptionPlanRequest("Audit Renamed", [new("Monthly", 7500m)], Reason: "Price review");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var updated = await Client.PutAsJsonAsync(EndpointUrl.AdminSubscriptionPlans.PlanV1(plan.Id), update);
            updated.StatusCode.ShouldBe(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        }

        using (var deactivated = await Client.PutAsJsonAsync(
            EndpointUrl.AdminSubscriptionPlans.ActivationV1(plan.Id),
            new SetSubscriptionPlanActivationRequest(false, "Retired offer")))
        {
            deactivated.StatusCode.ShouldBe(HttpStatusCode.OK, await deactivated.Content.ReadAsStringAsync());
        }

        // Allocation replayed with the same idempotency key, then cancelled; plus a failed cancellation.
        var allocationRequest = TenantAllocation();
        var allocation = await AllocateSuccessfullyAsync(allocationRequest);
        using (var replay = await AllocateAsync(allocationRequest))
        {
            replay.StatusCode.ShouldBe(HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        }

        using (var cancelled = await Client.PostAsJsonAsync(
            EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(allocation.SubscriptionId),
            new AdminSubscriptionCancellationRequest(TargetTenantId, "Contract terminated")))
        {
            cancelled.StatusCode.ShouldBe(HttpStatusCode.OK, await cancelled.Content.ReadAsStringAsync());
        }

        using (var missing = await Client.PostAsJsonAsync(
            EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(Guid.CreateVersion7()),
            new AdminSubscriptionCancellationRequest(TargetTenantId, "Missing subscription")))
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // An administrator performing an ordinary user operation is not an administrative action.
        using (var preferences = await Client.PutAsJsonAsync(
            $"{EndpointUrl.Stakeholders.V1}/me/preferences",
            new UpdatePreferencesRequest("dark", "en")))
        {
            preferences.StatusCode.ShouldBe(HttpStatusCode.NoContent, await preferences.Content.ReadAsStringAsync());
        }

        var all = await ListAsync($"actorUserId={AdminUserId}&pageSize=100");
        all.TotalCount.ShouldBe(5);
        all.Items.Select(item => item.Action).ShouldBe(
        [
            AdministrativeAuditActions.Subscriptions.Cancelled,
            AdministrativeAuditActions.Subscriptions.Allocated,
            AdministrativeAuditActions.SubscriptionPlans.Deactivated,
            AdministrativeAuditActions.SubscriptionPlans.Updated,
            AdministrativeAuditActions.SubscriptionPlans.Created
        ]);
        all.Items.ShouldAllBe(item =>
            item.Actor.UserId == AdminUserId &&
            item.Actor.StakeholderId == AdminStakeholderId &&
            item.Actor.DisplayName == "Grace Hopper" &&
            item.ActorTenantId == AdminTenantId);
        all.Items.Select(item => item.CorrelationId).Distinct().Count().ShouldBe(5);

        var page = await ListAsync($"actorUserId={AdminUserId}&page=2&pageSize=2");
        page.TotalCount.ShouldBe(5);
        page.Page.ShouldBe(2);
        page.Items.Select(item => item.Id).ShouldBe(all.Items.Skip(2).Take(2).Select(item => item.Id));

        // Actor tenant and target tenant differ: the allocation affects the customer's tenant.
        var allocated = all.Items.Single(item => item.Action == AdministrativeAuditActions.Subscriptions.Allocated);
        allocated.Target.ShouldBe(new AdministrativeAuditTargetSummary(
            AdministrativeAuditTargetTypes.Subscription, allocation.SubscriptionId, "Integration Base"));
        allocated.TargetTenantId.ShouldBe(TargetTenantId);
        allocated.Reason.ShouldBe("Integration allocation");
        allocated.Changes.ShouldNotBeNull();
        allocated.Changes.Value.GetProperty("planCode").GetProperty("current").GetString().ShouldBe($"{PlanCodePrefix}_BASE");
        allocated.Changes.Value.GetProperty("planCode").GetProperty("previous").ValueKind.ShouldBe(JsonValueKind.Null);

        var cancelledRecord = all.Items[0];
        cancelledRecord.TargetTenantId.ShouldBe(TargetTenantId);
        cancelledRecord.Reason.ShouldBe("Contract terminated");
        cancelledRecord.Changes!.Value.GetProperty("status").GetProperty("current").GetString().ShouldBe("Cancelled");

        // Global plan records preserve the name each action saw and JSON primitive types.
        var updatedRecord = all.Items.Single(item => item.Action == AdministrativeAuditActions.SubscriptionPlans.Updated);
        updatedRecord.TargetTenantId.ShouldBeNull();
        updatedRecord.Changes!.Value.GetProperty("prices").GetProperty("previous").GetProperty("Monthly").GetDecimal().ShouldBe(5000m);
        updatedRecord.Changes.Value.GetProperty("prices").GetProperty("current").GetProperty("Monthly").GetDecimal().ShouldBe(7500m);
        all.Items.Single(item => item.Action == AdministrativeAuditActions.SubscriptionPlans.Created)
            .Target.DisplayName.ShouldBe("Audit Original");
        all.Items.Single(item => item.Action == AdministrativeAuditActions.SubscriptionPlans.Deactivated)
            .Changes!.Value.GetProperty("isActive").GetProperty("current").GetBoolean().ShouldBeFalse();

        (await ListAsync($"actorUserId={AdminUserId}&action={AdministrativeAuditActions.Subscriptions.Allocated}"))
            .Items.Single().Id.ShouldBe(allocated.Id);
        (await ListAsync($"actorUserId={AdminUserId}&tenantId={TargetTenantId}")).TotalCount.ShouldBe(2);
        (await ListAsync($"actorUserId={AdminUserId}&tenantId={ForeignTenantId}")).Items.ShouldBeEmpty();
        (await ListAsync($"targetType={AdministrativeAuditTargetTypes.SubscriptionPlan}&targetId={plan.Id}")).TotalCount.ShouldBe(3);
        (await ListAsync($"correlationId={Uri.EscapeDataString(allocated.CorrelationId)}")).Items.Single().Id.ShouldBe(allocated.Id);
        (await ListAsync($"actorUserId={AdminUserId}&search=audit%20orig")).Items.Single().Action
            .ShouldBe(AdministrativeAuditActions.SubscriptionPlans.Created);
        (await ListAsync($"actorUserId={AdminUserId}&search=retired%20OFFER")).Items.Single().Action
            .ShouldBe(AdministrativeAuditActions.SubscriptionPlans.Deactivated);
        (await ListAsync($"actorUserId={AdminUserId}&search=%25%25")).Items.ShouldBeEmpty();
        var from = Uri.EscapeDataString(allocated.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture));
        var to = Uri.EscapeDataString(allocated.OccurredAtUtc.AddTicks(10).ToString("O", CultureInfo.InvariantCulture));
        (await ListAsync($"actorUserId={AdminUserId}&fromUtc={from}&toUtc={to}")).Items.Single().Id.ShouldBe(allocated.Id);
        (await ListAsync($"actorUserId={AdminUserId}&tenantId={TargetTenantId}&action={AdministrativeAuditActions.Subscriptions.Cancelled}&search=terminated"))
            .Items.Single().Id.ShouldBe(cancelledRecord.Id);

        using (var invalid = await Client.GetAsync($"{EndpointUrl.AdminAuditLogs.V1}?action=users.deleted&pageSize=500"))
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await invalid.Content.ReadAsStringAsync();
            problem.ShouldContain("Action");
            problem.ShouldContain("PageSize");
        }

        using (var raw = await Client.GetAsync($"{EndpointUrl.AdminAuditLogs.V1}?actorUserId={AdminUserId}&pageSize=100"))
        {
            var body = await raw.Content.ReadAsStringAsync();
            body.ShouldNotContain("@", Case.Insensitive);
            body.ShouldNotContain("password", Case.Insensitive);
            body.ShouldNotContain("securityStamp", Case.Insensitive);
        }

        // Records reference resources logically, so they stay readable after the resource is deleted.
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.SubscriptionPlanPrices.IgnoreQueryFilters().Where(price => price.PlanId == plan.Id).ExecuteDeleteAsync();
            await dbContext.SubscriptionPlans.IgnoreQueryFilters().Where(item => item.Id == plan.Id).ExecuteDeleteAsync();
        }

        (await ListAsync($"targetId={plan.Id}")).Items.Select(item => item.Target.DisplayName)
            .ShouldBe(["Audit Renamed", "Audit Renamed", "Audit Original"]);

        // Permission checks run against the active session: a revoked session loses access.
        using (var scope = CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAuthenticationSessionService>()
                .RevokeAllAsync(AdminStakeholderId, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync();
        }

        using var revoked = await Client.GetAsync(EndpointUrl.AdminAuditLogs.V1);
        revoked.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    private async Task<AdministrativeAuditListPage> ListAsync(string query)
    {
        using var response = await Client.GetAsync($"{EndpointUrl.AdminAuditLogs.V1}?{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AdministrativeAuditListPage>())!;
    }
}
