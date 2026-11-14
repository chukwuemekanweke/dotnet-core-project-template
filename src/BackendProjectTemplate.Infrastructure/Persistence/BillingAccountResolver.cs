using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Exceptions;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using BackendProjectTemplate.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class BillingAccountResolver(
    AppDbContext dbContext,
    IRepository<BillingAccount> billingAccountRepository,
    IRepository<Tenant> tenantRepository,
    IRepository<Stakeholder> stakeholderRepository) : IBillingAccountResolver
{
    public async Task<BillingAccount> ResolveOrCreateAsync(
        Guid tenantId,
        BillingAccountOwnerType ownerType,
        Guid? stakeholderId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (!Enum.IsDefined(ownerType))
        {
            throw new ArgumentOutOfRangeException(nameof(ownerType), ownerType, "Billing account owner type is invalid.");
        }

        var tenant = await tenantRepository.FirstOrDefaultAsync(
            new TenantByIdSpecification(tenantId),
            cancellationToken)
            ?? throw new BillingAccountOwnerNotFoundException($"Tenant '{tenantId}' is not available for billing.");

        var displayName = tenant.Name;
        if (ownerType == BillingAccountOwnerType.Tenant)
        {
            if (stakeholderId.HasValue)
            {
                throw new ArgumentException("A tenant-owned billing account cannot have a stakeholder id.", nameof(stakeholderId));
            }
        }
        else
        {
            if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
            {
                throw new ArgumentException("A stakeholder-owned billing account requires a stakeholder id.", nameof(stakeholderId));
            }

            var stakeholder = await stakeholderRepository.FirstOrDefaultAsync(
                new StakeholderByTenantAndIdSpecification(tenantId, stakeholderId.Value),
                cancellationToken);
            if (stakeholder is null)
            {
                var stakeholderInAnotherTenant = await stakeholderRepository.GetByIdAsync(stakeholderId.Value, cancellationToken);
                if (stakeholderInAnotherTenant is not null)
                {
                    throw new BillingAccountResolutionException(
                        $"Stakeholder '{stakeholderId}' does not belong to tenant '{tenantId}'.");
                }

                throw new BillingAccountOwnerNotFoundException(
                    $"Stakeholder '{stakeholderId}' is not available for billing.");
            }

            displayName = $"{stakeholder.FirstName} {stakeholder.LastName}";
        }

        var ownerSpecification = new BillingAccountByOwnerSpecification(tenantId, ownerType, stakeholderId);
        var existingAccount = await billingAccountRepository.FirstOrDefaultAsync(ownerSpecification, cancellationToken);
        if (existingAccount is not null)
        {
            return existingAccount;
        }

        var account = BillingAccount.Create(tenantId, ownerType, stakeholderId, displayName);
        await billingAccountRepository.AddAsync(account, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException exception) when (IsOwnerUniquenessConflict(exception))
        {
            dbContext.Entry(account).State = EntityState.Detached;

            var winningAccount = await billingAccountRepository.FirstOrDefaultAsync(ownerSpecification, cancellationToken);
            if (winningAccount is null)
            {
                throw new BillingAccountResolutionException(
                    "The billing account owner already has a retained account that cannot be replaced.",
                    exception);
            }

            return winningAccount;
        }
    }

    private static bool IsOwnerUniquenessConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: BillingAccountConfiguration.TenantOwnerUniqueIndexName
                or BillingAccountConfiguration.StakeholderOwnerUniqueIndexName
        };
}
