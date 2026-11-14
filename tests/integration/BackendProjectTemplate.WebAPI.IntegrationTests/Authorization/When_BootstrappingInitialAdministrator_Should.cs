using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Infrastructure.Authorization;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Authorization;

[Collection(nameof(ContainersCollection))]
public sealed class When_BootstrappingInitialAdministrator_Should(ContainersFixture fixture)
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
    public async Task AssignOnlyOnceToAnExistingVerifiedUser()
    {
        using var scope = CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        _user = AppUser.Create(WebApiIntegrationTestData.Email());
        (await userManager.CreateAsync(_user, "P@ssw0rd123!")).Succeeded.ShouldBeTrue();
        _user.MarkEmailVerified();
        (await userManager.UpdateAsync(_user)).Succeeded.ShouldBeTrue();
        var seeder = new ApplicationAuthorizationSeeder(
            database,
            roleManager,
            userManager,
            Options.Create(new AdministratorBootstrapOptions
            {
                Enabled = true,
                UserId = _user.Id
            }),
            scope.ServiceProvider.GetRequiredService<ILogger<ApplicationAuthorizationSeeder>>());

        await seeder.SeedAsync(CancellationToken.None);
        (await userManager.IsInRoleAsync(_user, ApplicationRoles.Administrator)).ShouldBeTrue();

        (await userManager.RemoveFromRoleAsync(_user, ApplicationRoles.Administrator)).Succeeded.ShouldBeTrue();
        await seeder.SeedAsync(CancellationToken.None);

        (await userManager.IsInRoleAsync(_user, ApplicationRoles.Administrator)).ShouldBeFalse();
    }
}
