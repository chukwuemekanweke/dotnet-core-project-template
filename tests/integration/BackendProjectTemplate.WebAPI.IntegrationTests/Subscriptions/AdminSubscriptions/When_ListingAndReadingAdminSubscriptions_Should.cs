using BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;
using BackendProjectTemplate.Domain.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingAndReadingAdminSubscriptions_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PageFilterAndReturnCompleteDetail()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        await AuthenticateAsync();
        var now = UtcNow();
        var active = await AllocateSuccessfullyAsync(TenantAllocation(endAtUtc: now.AddDays(10)));
        var scheduled = await AllocateSuccessfullyAsync(TenantAllocation(now.AddDays(20), now.AddDays(30), source: "Trial"));
        var stakeholder = await AllocateSuccessfullyAsync(StakeholderAllocation(TargetStakeholderId));

        var firstPage = await ListAsync($"TenantId={TargetTenantId}&Limit=2");
        firstPage.Items.Count.ShouldBe(2);
        firstPage.NextCursor.ShouldNotBeNull();
        var secondPage = await ListAsync($"TenantId={TargetTenantId}&Limit=2&Cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        secondPage.Items.Count.ShouldBe(1);
        secondPage.NextCursor.ShouldBeNull();
        firstPage.Items.Concat(secondPage.Items).Select(item => item.SubscriptionId)
            .ShouldBe([stakeholder.SubscriptionId, scheduled.SubscriptionId, active.SubscriptionId], ignoreOrder: true);

        (await ListAsync($"TenantId={TargetTenantId}&Status=Scheduled")).Items.ShouldHaveSingleItem().SubscriptionId.ShouldBe(scheduled.SubscriptionId);
        (await ListAsync($"TenantId={TargetTenantId}&AllocationSource=Trial")).Items.ShouldHaveSingleItem().AllocationSource.ShouldBe("Trial");
        var stakeholderItem = (await ListAsync($"TenantId={TargetTenantId}&BillingAccountOwnerType=Stakeholder&StakeholderId={TargetStakeholderId}"))
            .Items.ShouldHaveSingleItem();
        stakeholderItem.BillingAccountOwnerType.ShouldBe("Stakeholder");
        stakeholderItem.Origin.ShouldBe("Allocation");
        (await ListAsync($"TenantId={TargetTenantId}&AutoRenew=true")).Items.ShouldBeEmpty();
        using (var invalid = await Client.GetAsync($"{EndpointUrl.AdminSubscriptions.V1}?BillingAccountOwnerType=Wallet"))
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var detailResponse = await Client.GetAsync(EndpointUrl.AdminSubscriptions.DetailV1(stakeholder.SubscriptionId));
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<SubscriptionDetailResponse>())!;
        detail.Subscription.TenantId.ShouldBe(TargetTenantId);
        detail.Subscription.Status.ShouldBe("Active");
        detail.Stakeholder!.StakeholderId.ShouldBe(TargetStakeholderId);
        detail.Plan.Id.ShouldBe(PlanId);
        detail.Purchase.ShouldBeNull();
        detail.Payments.ShouldBeEmpty();
        detail.Renewals.ShouldBeEmpty();
        detail.Periods.ShouldHaveSingleItem().ActivationSource.ShouldBe("AdministrativeAllocation");
        var history = detail.StatusHistory.ShouldHaveSingleItem();
        history.ActorId.ShouldBe(AdminStakeholderId.ToString());
        history.Details.ShouldBe("Integration promotion");
        var allocation = detail.Allocations.ShouldHaveSingleItem();
        allocation.AllocationSource.ShouldBe("Promotion");
        allocation.AllocatedByActorId.ShouldBe(AdminStakeholderId.ToString());
        using var missing = await Client.GetAsync(EndpointUrl.AdminSubscriptions.DetailV1(Guid.CreateVersion7()));
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<ListSubscriptionsResult> ListAsync(string query)
    {
        using var response = await Client.GetAsync($"{EndpointUrl.AdminSubscriptions.V1}?{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ListSubscriptionsResult>())!;
    }
}
