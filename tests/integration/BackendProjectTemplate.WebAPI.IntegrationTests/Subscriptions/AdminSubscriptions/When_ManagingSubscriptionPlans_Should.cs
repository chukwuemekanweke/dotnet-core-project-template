using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_ManagingSubscriptionPlans_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task CreateUpdateAndDeactivatePlansWithoutChangingGrantedSnapshots()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        await AuthenticateAsync();
        var code = $"{PlanCodePrefix}_gold";

        using var createResponse = await Client.PostAsJsonAsync(
            EndpointUrl.AdminSubscriptionPlans.V1,
            new CreateSubscriptionPlanRequest(
                code,
                "Gold",
                [new("Monthly", 90000m), new("Yearly", 900000m)],
                CurrencyId,
                Description: "For larger teams.",
                Features: ["Unlimited team members", "Dedicated support"]));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        var plan = (await createResponse.Content.ReadFromJsonAsync<AdminSubscriptionPlanResponse>())!;
        plan.Code.ShouldBe(code.ToUpperInvariant());
        plan.Prices.Select(price => (price.BillingInterval, price.Price)).ShouldBe([("Monthly", 90000m), ("Yearly", 900000m)]);
        plan.Description.ShouldBe("For larger teams.");
        plan.Features.ShouldBe(["Unlimited team members", "Dedicated support"]);

        using (var duplicate = await Client.PostAsJsonAsync(
                   EndpointUrl.AdminSubscriptionPlans.V1,
                   new CreateSubscriptionPlanRequest(code, "Gold again", [new("Yearly", 900000m)], CurrencyId)))
        {
            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        using (var unknownCurrency = await Client.PostAsJsonAsync(
                   EndpointUrl.AdminSubscriptionPlans.V1,
                   new CreateSubscriptionPlanRequest($"{PlanCodePrefix}_x", "X", [new("Monthly", 1m)], Guid.CreateVersion7())))
        {
            unknownCurrency.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        using (var ambiguousInterval = await AllocateAsync(TenantAllocation() with { PlanId = plan.Id }))
        {
            ambiguousInterval.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        var grant = await AllocateSuccessfullyAsync(TenantAllocation() with { PlanId = plan.Id, BillingInterval = "Yearly" });

        using (var update = await Client.PutAsJsonAsync(
                   EndpointUrl.AdminSubscriptionPlans.PlanV1(plan.Id),
                   new UpdateSubscriptionPlanRequest("Gold Plus", [new("Monthly", 110000m), new("Yearly", 1100000m)])))
        {
            update.StatusCode.ShouldBe(HttpStatusCode.OK);
            var updated = (await update.Content.ReadFromJsonAsync<AdminSubscriptionPlanResponse>())!;
            updated.Prices.Single(price => price.BillingInterval == "Yearly").Price.ShouldBe(1100000m);
            updated.Features.ShouldBeEmpty();
            updated.Code.ShouldBe(plan.Code);
        }

        using (var deactivate = await Client.PutAsJsonAsync(
                   EndpointUrl.AdminSubscriptionPlans.ActivationV1(plan.Id),
                   new SetSubscriptionPlanActivationRequest(false)))
        {
            deactivate.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await deactivate.Content.ReadFromJsonAsync<AdminSubscriptionPlanResponse>())!.IsActive.ShouldBeFalse();
        }

        using (var withdrawInterval = await Client.PutAsJsonAsync(
                   EndpointUrl.AdminSubscriptionPlans.PlanV1(plan.Id),
                   new UpdateSubscriptionPlanRequest("Gold Plus", [new("Yearly", 1100000m)])))
        {
            withdrawInterval.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using (var allocateInactive = await AllocateAsync(StakeholderAllocation(TargetStakeholderId) with { PlanId = plan.Id, BillingInterval = "Yearly" }))
        {
            allocateInactive.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        using (var list = await Client.GetAsync($"{EndpointUrl.AdminSubscriptionPlans.V1}?IsActive=false"))
        {
            list.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await list.Content.ReadFromJsonAsync<List<AdminSubscriptionPlanResponse>>())!.ShouldContain(item => item.Id == plan.Id);
        }

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subscription = await dbContext.Subscriptions.SingleAsync(item => item.Id == grant.SubscriptionId);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        (await dbContext.SubscriptionPeriods.SingleAsync(item => item.SubscriptionId == grant.SubscriptionId)).Price.ShouldBe(900000m);
        (await dbContext.SubscriptionAllocations.SingleAsync(item => item.SubscriptionId == grant.SubscriptionId)).NominalPrice.ShouldBe(900000m);
    }
}
