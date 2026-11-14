using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Payments.BillingAccounts;

[Collection(nameof(ContainersCollection))]
public sealed class When_ResolvingBillingAccount_Concurrently_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override async Task InitializeWorkerTestAsync()
    {
        _tenantId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var tenant = Tenant.Create(_tenantId, "Concurrent Tenant", $"concurrent-{_tenantId:N}");
        var user = AppUser.Create($"concurrent-{Guid.CreateVersion7():N}@example.com", "Concurrent", "Owner");
        var stakeholderType = StakeholderType.Create(_tenantId, "Individual Customer", "individual-customer");
        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Concurrent",
            "Owner");

        dbContext.Tenants.Add(tenant);
        dbContext.StakeholderTypes.Add(stakeholderType);
        dbContext.Stakeholders.Add(stakeholder);
        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.BillingAccounts
            .Where(account => account.TenantId == _tenantId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == _stakeholderId)
            .ExecuteDeleteAsync();
        await scope.DbContext.StakeholderTypes
            .Where(stakeholderType => stakeholderType.Id == _stakeholderTypeId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Users
            .Where(user => user.Id == _userId)
            .ExecuteDeleteAsync();
        await scope.DbContext.Tenants
            .Where(tenant => tenant.Id == _tenantId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task ReturnTheSinglePersistedAccount()
    {
        var firstStakeholderResolution = ResolveAsync(BillingAccountOwnerType.Stakeholder, _stakeholderId);
        var secondStakeholderResolution = ResolveAsync(BillingAccountOwnerType.Stakeholder, _stakeholderId);
        var firstTenantResolution = ResolveAsync(BillingAccountOwnerType.Tenant, null);
        var secondTenantResolution = ResolveAsync(BillingAccountOwnerType.Tenant, null);

        var accounts = await Task.WhenAll(
            firstStakeholderResolution,
            secondStakeholderResolution,
            firstTenantResolution,
            secondTenantResolution);

        accounts[0].Id.ShouldBe(accounts[1].Id);
        accounts[2].Id.ShouldBe(accounts[3].Id);
        accounts[0].Id.ShouldNotBe(accounts[2].Id);
        using var assertionScope = CreateDbContextScope();
        (await assertionScope.DbContext.BillingAccounts.CountAsync(account => account.TenantId == _tenantId)).ShouldBe(2);
    }

    private async Task<Domain.Payments.Entities.BillingAccount> ResolveAsync(
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId)
    {
        using var scope = CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IBillingAccountResolver>();
        return await resolver.ResolveOrCreateAsync(
            _tenantId,
            ownerType,
            stakeholderId,
            CancellationToken.None);
    }
}
