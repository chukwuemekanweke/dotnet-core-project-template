using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class WalletByStakeholderAndCurrencySpecification : Specification<Wallet>
{
    public WalletByStakeholderAndCurrencySpecification(Guid stakeholderId, Guid currencyId)
    {
        Where(wallet => wallet.StakeholderId == stakeholderId && wallet.CurrencyId == currencyId);
        EnableTracking();
    }
}
