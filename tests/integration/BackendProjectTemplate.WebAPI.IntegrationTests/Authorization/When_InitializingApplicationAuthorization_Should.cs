using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Authorization;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Security.Claims;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Authorization;

[Collection(nameof(ContainersCollection))]
public sealed class When_InitializingApplicationAuthorization_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private AppUser? _user;

    public Task InitializeAsync() => InitializeClientAsync();

    public async Task DisposeAsync()
    {
        if (_user is not null)
        {
            using var scope = CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByIdAsync(_user.Id.ToString());
            if (user is not null)
            {
                (await userManager.DeleteAsync(user)).Succeeded.ShouldBeTrue();
            }
        }

        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReconcileRolesAndPermissionsIdempotentlyWithoutRemovingUserRoles()
    {
        using var scope = CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var seeder = scope.ServiceProvider.GetRequiredService<IApplicationAuthorizationSeeder>();
        _user = AppUser.Create(WebApiIntegrationTestData.Email());
        (await userManager.CreateAsync(_user, "P@ssw0rd123!")).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(_user, ApplicationRoles.FinanceOfficer)).Succeeded.ShouldBeTrue();

        var financeRole = await roleManager.FindByNameAsync(ApplicationRoles.FinanceOfficer);
        financeRole.ShouldNotBeNull();
        (await roleManager.AddClaimAsync(
            financeRole,
            new Claim(CustomClaimTypes.Permission, "obsolete.permission"))).Succeeded.ShouldBeTrue();

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        foreach (var roleName in ApplicationRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            role.ShouldNotBeNull();
            var permissionClaims = (await roleManager.GetClaimsAsync(role))
                .Where(claim => claim.Type == CustomClaimTypes.Permission)
                .Select(claim => claim.Value)
                .ToArray();
            permissionClaims.Length.ShouldBe(permissionClaims.Distinct(StringComparer.Ordinal).Count());
            permissionClaims.ToHashSet(StringComparer.Ordinal)
                .SetEquals(RolePermissionMappings.ForRole(roleName))
                .ShouldBeTrue();
        }

        (await userManager.IsInRoleAsync(_user, ApplicationRoles.FinanceOfficer)).ShouldBeTrue();
    }
}
