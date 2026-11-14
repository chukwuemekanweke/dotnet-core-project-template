using BackendProjectTemplate.Consumer.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace BackendProjectTemplate.Consumer.IntegrationTests.Payments.BillingAccounts;

[Collection(nameof(ContainersCollection))]
public sealed class When_PersistingBillingAccount_WithCrossTenantStakeholder_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _ownerTenantId;
    private Guid _otherTenantId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _userId;

    protected override async Task InitializeWorkerTestAsync()
    {
        _ownerTenantId = Guid.CreateVersion7();
        _otherTenantId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var ownerTenant = Tenant.Create(_ownerTenantId, "Owner Tenant", $"owner-{_ownerTenantId:N}");
        var otherTenant = Tenant.Create(_otherTenantId, "Other Tenant", $"other-{_otherTenantId:N}");
        var user = AppUser.Create($"cross-tenant-{Guid.CreateVersion7():N}@example.com", "Cross", "Tenant");
        var stakeholderType = StakeholderType.Create(_ownerTenantId, "Individual Customer", "individual-customer");
        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();
        var stakeholder = Stakeholder.Create(
            user.Id,
            _ownerTenantId,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Cross",
            "Tenant");

        dbContext.Tenants.AddRange(ownerTenant, otherTenant);
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
            .Where(account => account.TenantId == _ownerTenantId || account.TenantId == _otherTenantId)
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
            .Where(tenant => tenant.Id == _ownerTenantId || tenant.Id == _otherTenantId)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task FailTheCompositeForeignKey()
    {
        using var scope = CreateDbContextScope();
        scope.DbContext.BillingAccounts.Add(BillingAccount.Create(
            _otherTenantId,
            BillingAccountOwnerType.Stakeholder,
            _stakeholderId,
            "Invalid Cross-Tenant Owner"));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => scope.DbContext.SaveChangesAsync());

        var postgresException = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
    }
}
