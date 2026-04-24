using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class ActiveCurrencyByIdSpecification : Specification<Currency>
{
    public ActiveCurrencyByIdSpecification(Guid currencyId)
    {
        Where(currency => currency.Id == currencyId && currency.IsActive);
    }
}
