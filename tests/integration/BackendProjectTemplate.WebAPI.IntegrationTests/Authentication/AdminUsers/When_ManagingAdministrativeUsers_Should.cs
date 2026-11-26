using BackendProjectTemplate.Application.Authentication.Features.GetAdminUserRoles;
using BackendProjectTemplate.Application.Authentication.Features.SignIn;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Authentication;
using BackendProjectTemplate.Domain.Authentication.Administration;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.Features.Authentication.AdminUsers;
using BackendProjectTemplate.WebAPI.Features.Authentication.Sessions;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Authentication.AdminUsers;

[Collection(nameof(ContainersCollection))]
public sealed class When_ManagingAdministrativeUsers_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private Guid _targetUserId;
    private string _targetEmail = string.Empty;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var target = await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == TargetStakeholderId)
            .Select(stakeholder => new { stakeholder.AppUserId, stakeholder.AppUser.Email })
            .SingleAsync();
        _targetUserId = target.AppUserId;
        _targetEmail = target.Email!;
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.AdministrativeSecurityAudits
                .IgnoreQueryFilters()
                .Where(audit => audit.ActorUserId == AdminUserId || audit.TargetUserId == _targetUserId)
                .ExecuteDeleteAsync();
        }

        await base.DisposeAsync();
    }

    [Fact]
    public async Task EnforcePermissionsProjectSafeDataAssignKnownRolesAndLockAccounts()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.AdminUsers.V1))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await AuthenticateAsync();
        await GrantRoleAsync(ApplicationRoles.SupportAgent);
        using (var readable = await Client.GetAsync(EndpointUrl.AdminUsers.V1))
        {
            readable.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using (var forbidden = await Client.PostAsJsonAsync(
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/lock",
            new AdminSecurityMutationRequest("Authorization probe")))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await RevokeRoleAsync(ApplicationRoles.SupportAgent);
        await GrantRoleAsync(ApplicationRoles.Administrator);

        using (var list = await Client.GetAsync(
            $"{EndpointUrl.AdminUsers.V1}?Search={Uri.EscapeDataString(_targetEmail)}&TenantId={TargetTenantId}&Page=1&PageSize=10"))
        {
            list.StatusCode.ShouldBe(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());
            var page = await list.Content.ReadFromJsonAsync<AdminUserListPage>();
            page.ShouldNotBeNull();
            page.TotalCount.ShouldBe(1);
            page.Items.Single().UserId.ShouldBe(_targetUserId);
        }

        using (var unknownRole = await Client.PutAsJsonAsync(
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/roles/UnknownRole",
            new AdminSecurityMutationRequest("Invalid role probe")))
        {
            unknownRole.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await unknownRole.Content.ReadAsStringAsync());
        }

        using (var assign = await Client.PutAsJsonAsync(
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/roles/{ApplicationRoles.SupportAgent}",
            new AdminSecurityMutationRequest("Support duties")))
        {
            assign.StatusCode.ShouldBe(HttpStatusCode.OK, await assign.Content.ReadAsStringAsync());
        }

        using (var roles = await Client.GetAsync($"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/roles"))
        {
            var payload = await roles.Content.ReadFromJsonAsync<GetAdminUserRolesResponse>();
            roles.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload!.Roles.ShouldContain(ApplicationRoles.SupportAgent);
        }

        using var targetClient = CreateRawClient();
        targetClient.DefaultRequestHeaders.Add("X-Tenant-Id", TargetTenantId.ToString());
        using var targetSignIn = await targetClient.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_targetEmail, Password));
        targetSignIn.StatusCode.ShouldBe(HttpStatusCode.OK);
        var targetTokens = await targetSignIn.Content.ReadFromJsonAsync<SignInResponse>();
        targetClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", targetTokens!.AccessToken);

        using (var revokeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/sessions")
        {
            Content = JsonContent.Create(new AdminSecurityMutationRequest("Revoke compromised sessions"))
        })
        using (var revoke = await Client.SendAsync(revokeRequest))
        {
            revoke.StatusCode.ShouldBe(HttpStatusCode.OK, await revoke.Content.ReadAsStringAsync());
        }

        using (var deniedAccess = await targetClient.GetAsync($"{EndpointUrl.Stakeholders.V1}/me/profile"))
        {
            deniedAccess.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        using (var deniedRefresh = await targetClient.PostAsJsonAsync(
            EndpointUrl.Sessions.RefreshV1,
            new RefreshSessionRequest(targetTokens.RefreshToken!)))
        {
            deniedRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using (var locked = await Client.PostAsJsonAsync(
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/lock",
            new AdminSecurityMutationRequest("Confirmed account compromise")))
        {
            locked.StatusCode.ShouldBe(HttpStatusCode.OK, await locked.Content.ReadAsStringAsync());
        }

        targetClient.DefaultRequestHeaders.Authorization = null;
        using (var signIn = await targetClient.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_targetEmail, Password)))
        {
            signIn.StatusCode.ShouldBe(HttpStatusCode.Locked);
        }

        using (var unlocked = await Client.PostAsJsonAsync(
            $"{EndpointUrl.AdminUsers.DetailV1(_targetUserId)}/unlock",
            new AdminSecurityMutationRequest("Identity verified")))
        {
            unlocked.StatusCode.ShouldBe(HttpStatusCode.OK, await unlocked.Content.ReadAsStringAsync());
        }

        using (var signInAfterUnlock = await targetClient.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_targetEmail, Password)))
        {
            signInAfterUnlock.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var verificationScope = CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verificationDb.Users.SingleAsync(user => user.Id == _targetUserId))
            .IsAdministrativelyLocked.ShouldBeFalse();
        var audits = await verificationDb.AdministrativeSecurityAudits
            .Where(audit => audit.TargetUserId == _targetUserId)
            .ToListAsync();
        audits.Select(audit => audit.Action).ShouldContain(AdministrativeSecurityAction.AssignRole);
        audits.Select(audit => audit.Action).ShouldContain(AdministrativeSecurityAction.LockUser);
        audits.Select(audit => audit.Action).ShouldContain(AdministrativeSecurityAction.UnlockUser);
        audits.Select(audit => audit.Action).ShouldContain(AdministrativeSecurityAction.RevokeAllSessions);

        var unified = await verificationDb.AdministrativeAudits
            .Where(audit => audit.TargetId == _targetUserId)
            .OrderBy(audit => audit.OccurredAtUtc)
            .ToListAsync();
        unified.Select(audit => audit.Action).ShouldBe(
        [
            AdministrativeAuditActions.Users.RoleAssigned,
            AdministrativeAuditActions.Users.AllSessionsRevoked,
            AdministrativeAuditActions.Users.Locked,
            AdministrativeAuditActions.Users.Unlocked
        ]);
        unified.ShouldAllBe(audit =>
            audit.ActorUserId == AdminUserId &&
            audit.ActorStakeholderId == AdminStakeholderId &&
            audit.TargetType == AdministrativeAuditTargetTypes.User &&
            audit.TargetTenantId == null &&
            audit.ActorTenantId == AdminTenantId);
        unified[0].Reason.ShouldBe("Support duties");
        unified[0].Changes.ShouldBe("{\"roles\": {\"current\": [\"SupportAgent\"], \"previous\": []}}");
        unified[2].Changes.ShouldBe("{\"isAdministrativelyLocked\": {\"current\": true, \"previous\": false}}");
        unified[1].Changes.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeConcurrentRemovalOfTheLastTwoAdministrators()
    {
        await GrantRoleAsync(ApplicationRoles.Administrator);
        using (var seedScope = CreateScope())
        {
            var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var target = await userManager.FindByIdAsync(_targetUserId.ToString());
            (await userManager.AddToRoleAsync(target!, ApplicationRoles.Administrator)).Succeeded.ShouldBeTrue();
        }

        using var firstScope = CreateScope();
        using var secondScope = CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IAdminUserRoleAdministration>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IAdminUserRoleAdministration>();
        var actorContext = new BackendProjectTemplate.Domain.Common.Auditing.ActorContext(
            null, AdminTenantId, "concurrent-admin-removal", "concurrent-admin-removal");

        var results = await Task.WhenAll(
            firstService.RemoveAsync(new AdminUserRoleRequest(
                AdminUserId, _targetUserId, ApplicationRoles.Administrator,
                "Concurrent safety test", actorContext), CancellationToken.None),
            secondService.RemoveAsync(new AdminUserRoleRequest(
                _targetUserId, AdminUserId, ApplicationRoles.Administrator,
                "Concurrent safety test", actorContext), CancellationToken.None));

        results.Count(result => result.Status == AdminUserMutationStatus.Succeeded).ShouldBe(1);
        results.Count(result => result.Status == AdminUserMutationStatus.LastAdministrator).ShouldBe(1);

        using var verifyScope = CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var remaining = 0;
        foreach (var userId in new[] { AdminUserId, _targetUserId })
        {
            var user = await verifyUserManager.FindByIdAsync(userId.ToString());
            if (await verifyUserManager.IsInRoleAsync(user!, ApplicationRoles.Administrator)) remaining++;
        }
        remaining.ShouldBe(1);
    }
}
