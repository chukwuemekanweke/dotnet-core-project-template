using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;
using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;

[Collection(nameof(ContainersCollection))]
public sealed class When_AuthorizingAdminSubscriptionEndpoints_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    [Fact]
    public async Task SeparateReadAllocateManageAndPlanPermissions()
    {
        (await StatusesAsync()).ShouldBe([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized]);
        await AuthenticateAsync();
        (await StatusesAsync()).ShouldBe([HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden]);

        await GrantRoleAsync(ApplicationRoles.SupportAgent);
        (await StatusesAsync()).ShouldBe([HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden]);
        await RevokeRoleAsync(ApplicationRoles.SupportAgent);

        await GrantRoleAsync(ApplicationRoles.FinanceOfficer);
        (await StatusesAsync()).ShouldBe([HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Forbidden]);
        await RevokeRoleAsync(ApplicationRoles.FinanceOfficer);

        await GrantRoleAsync(ApplicationRoles.Administrator);
        (await StatusesAsync()).ShouldBe([HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.NotFound]);
        await RevokeRoleAsync(ApplicationRoles.Administrator);

        (await StatusesAsync()).ShouldBe([HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden]);
    }

    private async Task<HttpStatusCode[]> StatusesAsync()
    {
        using var read = await Client.GetAsync($"{EndpointUrl.AdminSubscriptions.V1}?TenantId={TargetTenantId}");
        using var allocate = await AllocateAsync(TenantAllocation() with { Reason = string.Empty });
        using var manage = await Client.PostAsJsonAsync(
            EndpointUrl.AdminSubscriptions.ImmediateCancellationV1(Guid.CreateVersion7()),
            new AdminSubscriptionCancellationRequest(TargetTenantId, "Authorization probe"));
        using var plans = await Client.PutAsJsonAsync(
            EndpointUrl.AdminSubscriptionPlans.ActivationV1(Guid.CreateVersion7()),
            new SetSubscriptionPlanActivationRequest(false));
        return [read.StatusCode, allocate.StatusCode, manage.StatusCode, plans.StatusCode];
    }
}
