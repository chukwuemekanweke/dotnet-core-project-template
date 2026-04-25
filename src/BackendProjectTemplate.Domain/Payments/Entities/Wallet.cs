using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Exceptions;

namespace BackendProjectTemplate.Domain.Payments.Entities;

public sealed class Wallet : Entity, IAggregateRoot
{
    private Wallet()
    {
    }

    private Wallet(Guid stakeholderId, Guid tenantId, Guid currencyId, decimal balance)
    {
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        CurrencyId = currencyId;
        Balance = balance;
    }

    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal Balance { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Wallet Create(Guid stakeholderId, Guid tenantId, Guid currencyId, DateTimeOffset utcNow) =>
        new(stakeholderId, tenantId, currencyId, 0m);

    public void Credit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new AggregateStateException("Wallet credit amount must be greater than zero.");
        }

        Balance += amount;
    }
}
