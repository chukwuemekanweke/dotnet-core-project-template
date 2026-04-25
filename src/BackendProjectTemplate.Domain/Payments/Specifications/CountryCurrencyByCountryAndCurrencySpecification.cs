using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.Payments.Specifications;

public sealed class CountryCurrencyByCountryAndCurrencySpecification : Specification<CountryCurrency>
{
    public CountryCurrencyByCountryAndCurrencySpecification(Guid countryId, Guid currencyId)
    {
        Where(mapping =>
            mapping.CountryId == countryId &&
            mapping.CurrencyId == currencyId &&
            mapping.IsActive);
    }
}
